﻿using BranchActualizer.Branches;
using SharpBucket.V2;
using SharpBucket.V2.EndPoints;
using SharpBucket.V2.Pocos;
using RepositoryInfo = BranchActualizer.Repositories.RepositoryInfo;

namespace BranchActualizer;

public class BitbucketBranchActualizerFactory: IBranchActualizerFactory
{
    private const int MaxPageLen = 100;

    private readonly IBranchMergeSolver _mergeSolver;
    
    private readonly IBranchAuthorResolver _authorResolver;

    private readonly SharpBucketV2 _bucket;
    
    private readonly BitBucketBranchActualizerSettings _settings;

    public BitbucketBranchActualizerFactory(IBranchMergeSolver mergeSolver, IBranchAuthorResolver authorResolver, SharpBucketV2 bucket, BitBucketBranchActualizerSettings settings)
    {
        _mergeSolver = mergeSolver;
        _authorResolver = authorResolver;
        _bucket = bucket;
        _settings = settings;
    }

    public IBranchActualizerFactory WithRepositories(IEnumerable<RepositoryInfo> repositories)
    {
        return new BitbucketBranchActualizerFactory(_mergeSolver, _authorResolver, _bucket, new BitBucketBranchActualizerSettings(_settings)
        {
            Repositories = repositories
        });
    }

    public async Task<IBranchActualizer> BuildBranchActualizerAsync(CancellationToken cancellationToken = default)
    {
        if (_settings.Repositories?.Any() is not true)
        {
            return null;
        }
        Console.WriteLine("Get repositories...");
        var repositoryResource = _bucket
            .WorkspacesEndPoint()
            .WorkspaceResource(_settings.WorkspaceSlugOrUuid) // "imscoua"
            .RepositoriesResource;

        Console.WriteLine("Get user branches...");

        var notActualizedBranchesTasks =
            _settings.Repositories.Select(repository => GetRepositoryWithNotActualizedBranches(repository, repositoryResource))
                .ToList();

        await Task.WhenAll(notActualizedBranchesTasks);

        var notActualizedBranches = notActualizedBranchesTasks
            .SelectMany(x => x.Result)
            .Select(async(x) => new BitBucketBranchActualizer.ExtendedBranchInfo()
            {
                Branch = x.branch,
                RepositoryResource = x.repositoryResource,
                Repository = x.repository,
                Source = x.source,
                Author = await _authorResolver.GetAuthorAsync(x.branch.name, cancellationToken)
            }).ToList();

        await Task.WhenAll(notActualizedBranches);

        return new BitBucketBranchActualizer(notActualizedBranches.Select(x => x.Result));
    }

    private async Task<IEnumerable<(Branch source, RepositoryResource repositoryResource, RepositoryInfo repository, Branch branch)>> GetRepositoryWithNotActualizedBranches(
        RepositoryInfo repository,
        RepositoriesAccountResource repositoryResource)
    {
        Console.WriteLine($"Start inspecting {repository.Name}");
        var currentRepo = repositoryResource.RepositoryResource(repository.Id);
        var branches = currentRepo.BranchesResource.ListBranches(new ListParameters()
        {
            Filter = AddDevelopAndMasterToFilter(await _mergeSolver.ToFilterAsync(new FilterInfo()
                { RepositorySlug = repository.Id }))
        });
        var foundBranches = await branches.ToAsyncEnumerable().WhereAwait(async(x) => await _mergeSolver.ShouldMergeAsync(new BranchInfo() { Name = x.name, Repository = repository.Id }))
            .ToListAsync();

        var develop = branches.FirstOrDefault(x => x.name.Equals("develop"));
        var master = branches.FirstOrDefault(x => x.name.Equals("master"));
        var foundMyBranches = foundBranches.Where(x => !x.name.Equals("develop") && !x.name.Equals("master")).ToList();

        return await GetNotActualizedBranches(foundMyBranches, develop, master, repository, currentRepo);
    }

    private async Task<IEnumerable<(Branch source, RepositoryResource repositoryResource, RepositoryInfo repository, Branch branch)>> GetNotActualizedBranches(List<Branch> allBranches,
        Branch develop, Branch master, RepositoryInfo repository,
        RepositoryResource repositoryResource)
    {
        Dictionary<(Branch source, RepositoryResource repositoryResource, RepositoryInfo repository, Branch branch), bool> actualizedBranches = new();
        foreach (var branch in allBranches)
        {
            Console.WriteLine($"Inspecting {branch.name} at {repository.Name}");
            Branch source;
            if (branch.name.Contains("hotfix") && master is not null)
                source = master;
            else if (branch.name.Contains("feature") && develop is not null)
                source = develop;
            else
            {
                Console.WriteLine("Cannot find develop or master branch...");
                continue;
            }

            actualizedBranches[(source, repositoryResource, repository, branch)] = repositoryResource.ListCommits(new ListCommitsParameters()
                {
                    Excludes = { branch.name },
                    Includes = { source.name },
                    Max = 1
                }) is {Count: 0};
        }

        return actualizedBranches.Where(x => !x.Value).Select(x => x.Key);
    }

    private string AddDevelopAndMasterToFilter(string filter)
    {
        return string.IsNullOrWhiteSpace(filter) ? string.Empty : $"{filter} OR name = \"develop\" OR name = \"master\"";
    }
}