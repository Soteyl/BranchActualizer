using Microsoft.Extensions.Logging;
using SharpBucket.V2;
using SharpBucket.V2.EndPoints;
using SharpBucket.V2.Pocos;
using RepositoryInfo = BranchActualizer.Repositories.RepositoryInfo;

namespace BranchActualizer;

public class BitBucketBranchActualizer: IBranchActualizer
{
    private readonly List<ExtendedBranchInfo> _branches;
    
    private readonly ILogger _logger;

    internal BitBucketBranchActualizer(IEnumerable<ExtendedBranchInfo> branches, ILogger<BitBucketBranchActualizer> logger)
    {
        _logger = logger;
        _branches = branches.ToList();
    }
    
    public IEnumerable<IBranchInfo> Branches => _branches;

    public async Task<ActualizeBranchesResult> ActualizeAsync(CancellationToken cancellationToken = default)
    {
        var conflicts = new List<PullRequestMergeConflict>();
        var successMerges = new List<IBranchInfo>();
        var cannotActualize = new List<CannotActualize>();
        foreach (var branch in _branches)
        {
            if (branch.Source is null)
            {
                cannotActualize.Add(new CannotActualize()
                {
                    BranchName = branch.Branch.name,
                    RepositoryName = branch.Repository?.Name,
                    AuthorId = branch?.Author,
                    Reason = "Cannot find source branch"
                });
                continue;
            }

            _logger.Log(LogLevel.Information, $"Actualizing {branch.Branch.name}");
            var pullRequestResource = branch.RepositoryResource.PullRequestsResource();
            PullRequest? pullRequest = null;
            try
            {
                pullRequest = await pullRequestResource.PostPullRequestAsync(new PullRequest
                {
                    title = $"Actualizing {branch.Branch.name}",
                    source = new Source { branch = branch.Source },
                    destination = new Source { branch = branch.Branch }
                }, cancellationToken);
                _logger.Log(LogLevel.Information, "Created pull request");
                await pullRequestResource.PullRequestResource(pullRequest.id!.Value)
                    .AcceptAndMergePullRequestAsync(cancellationToken);
                _logger.Log(LogLevel.Information, $"Merged {branch.Source.name} in {branch.Branch.name}");
                successMerges.Add(branch);
            }
            catch (BitbucketV2Exception ex)
            {
                conflicts.Add(new PullRequestMergeConflict
                {
                    BranchName = branch?.Branch?.name,
                    RepositoryName = branch?.Repository?.Name,
                    PullRequestId = pullRequest?.id?.ToString(),
                    PullRequestName = pullRequest?.title,
                    PullRequestLink = pullRequest?.links?.self?.href,
                    AuthorId = branch?.Author
                });
                if (pullRequest?.id is not null)
                {
                    await pullRequestResource.PullRequestResource(pullRequest.id.Value)
                        .DeclinePullRequestAsync(cancellationToken);
                }

                _logger.Log(LogLevel.Warning, $"Conflict while actualizing {branch?.Branch?.name}");
            }
            catch (Exception ex)
            {
                cannotActualize.Add(new CannotActualize()
                {
                    BranchName = branch?.Branch?.name,
                    RepositoryName = branch?.Repository?.Name,
                    AuthorId = branch?.Author,
                    Reason = ex.Message
                });
            }
        }

        return new ActualizeBranchesResult
            { Conflicts = conflicts, SuccessMerges = successMerges, CannotActualize = cannotActualize };
    }

    public Task ExcludeBranchesAsync(IEnumerable<IBranchInfo> branches, CancellationToken cancellationToken = default)
    {
        branches.Select(x => x as ExtendedBranchInfo).Where(x => x is not null).ToList().ForEach(x => _branches.Remove(x!));
        
        return Task.CompletedTask;
    }

    internal class ExtendedBranchInfo: IBranchInfo
    {
        public string? Author { get; set; }
        
        public RepositoryResource RepositoryResource { get; set; }
        
        public RepositoryInfo Repository { get; set; }
        
        public Branch? Source { get; set; }
        
        public Branch Branch { get; set; }

        string IBranchInfo.RepositoryName => Repository.Name;
        
        string IBranchInfo.BranchName => Branch.name;
    }
}