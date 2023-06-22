using SharpBucket.V2;
using SharpBucket.V2.EndPoints;

namespace BranchActualizer.Repositories;

public class BitBucketActualRepositoriesCacheContainer : IActualRepositoriesContainer
{
    private readonly SharpBucketV2 _bucket;
    private readonly TimeSpan _cacheTime;
    private DateTime _lastCached;
    private readonly string _workspaceSlugOrUuid;
    private readonly string _projectUuid;

    private IEnumerable<RepositoryInfo>? _repositories;

    public BitBucketActualRepositoriesCacheContainer(SharpBucketV2 bucket, TimeSpan cacheTime, string workspaceSlugOrUuid, string projectUuid)
    {
        _bucket = bucket;
        _cacheTime = cacheTime;
        _workspaceSlugOrUuid = workspaceSlugOrUuid;
        _projectUuid = projectUuid;
    }

    public async Task<IEnumerable<RepositoryInfo>> GetActualRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        if (_lastCached + _cacheTime >= DateTime.Now && _repositories is not null) return _repositories;
        
        var repositoryResource = _bucket
            .WorkspacesEndPoint()
            .WorkspaceResource(_workspaceSlugOrUuid) 
            .RepositoriesResource;
        _repositories = (await repositoryResource
                .EnumerateRepositoriesAsync(new EnumerateRepositoriesParameters()
                {
                    Filter = $"project.uuid = \"{_projectUuid}\"",
                    PageLen = 100
                }, cancellationToken).ToListAsync(cancellationToken))
            .Select(x => new RepositoryInfo()
            {
                Name = x.name,
                Id = x.slug
            });
        
        _lastCached = DateTime.Now;

        return _repositories.OrderByDescending(x => x.Name);
    }
}

public interface IActualRepositoriesContainer
{
    Task<IEnumerable<RepositoryInfo>> GetActualRepositoriesAsync(CancellationToken cancellationToken = default);
}