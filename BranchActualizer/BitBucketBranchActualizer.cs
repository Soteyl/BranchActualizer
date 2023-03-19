using SharpBucket.V2;
using SharpBucket.V2.EndPoints;
using SharpBucket.V2.Pocos;
using RepositoryInfo = BranchActualizer.Repositories.RepositoryInfo;

namespace BranchActualizer;

public class BitBucketBranchActualizer: IBranchActualizer
{
    private readonly List<ExtendedBranchInfo> _branches;

    internal BitBucketBranchActualizer(IEnumerable<ExtendedBranchInfo> branches)
    {
        _branches = branches.ToList();
    }
    
    public IEnumerable<IBranchInfo> Branches => _branches;

    public async Task<ActualizeBranchesResult> ActualizeAsync(CancellationToken cancellationToken = default)
    {
        var conflicts = new List<PullRequestMergeConflict>();
        var successMerges = new List<IBranchInfo>();
        foreach (var branch in _branches)
        {
            Console.WriteLine($"Actualizing {branch.Branch.name}");
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
                Console.WriteLine("Created pull request");
                await pullRequestResource.PullRequestResource(pullRequest.id!.Value).AcceptAndMergePullRequestAsync(cancellationToken);
                Console.WriteLine($"Merged {branch.Source.name} in {branch.Branch.name}");
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
                    await pullRequestResource.PullRequestResource(pullRequest.id.Value).DeclinePullRequestAsync(cancellationToken);
                }
            }
        }

        return new ActualizeBranchesResult { Conflicts = conflicts, SuccessMerges = successMerges };
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
        
        public Branch Source { get; set; }
        
        public Branch Branch { get; set; }

        string IBranchInfo.RepositoryName => Repository.Name;
        
        string IBranchInfo.BranchName => Branch.name;
    }
}