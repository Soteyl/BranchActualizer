using SharpBucket.V2;
using SharpBucket.V2.EndPoints;

namespace BranchActualizer.Repositories;

public class BitBucketActualRepositoriesCacheContainer : IActualRepositoriesContainer
{
    private readonly SharpBucketV2 _bucket;
    private readonly TimeSpan _cacheTime;
    private DateTime _lastCached;
    private readonly string _workspaceSlugOrUuid;
    private readonly string[] _projectNames;

    private List<RepositoryInfo>? _repositories;

    public BitBucketActualRepositoriesCacheContainer(SharpBucketV2 bucket, TimeSpan cacheTime, string workspaceSlugOrUuid, string[] projectNames)
    {
        _bucket = bucket;
        _cacheTime = cacheTime;
        _workspaceSlugOrUuid = workspaceSlugOrUuid;
        _projectNames = projectNames;
    }

    public async Task<IEnumerable<RepositoryInfo>> GetActualRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        if (_lastCached + _cacheTime >= DateTime.Now && _repositories is not null) return _repositories;
        
        var repositoryResource = _bucket
            .WorkspacesEndPoint()
            .WorkspaceResource(_workspaceSlugOrUuid) 
            .RepositoriesResource;

        _repositories = new List<RepositoryInfo>();
        
        foreach (var name in _projectNames)
        {
            _repositories.AddRange(repositoryResource
                    .ListRepositories(new ListRepositoriesParameters()
                    {
                        Filter = $"project.name = \"{name}\""
                    }).Select(x => new RepositoryInfo()
                    {
                        Name = x.name,
                        Id = x.slug
                    }));
        }
        
        _repositories = _repositories.OrderByDescending(x => x.Name).ToList();
        _lastCached = DateTime.Now;

        return _repositories;
    }
}

public interface IActualRepositoriesContainer
{
    Task<IEnumerable<RepositoryInfo>> GetActualRepositoriesAsync(CancellationToken cancellationToken = default);
}