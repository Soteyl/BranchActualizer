using BranchActualizer.Repositories;

namespace BranchActualizer;

public interface IBranchActualizerFactory
{
    IBranchActualizerFactory WithRepositories(IEnumerable<RepositoryInfo> repositories);
    
    Task<IBranchActualizer> BuildBranchActualizerAsync(CancellationToken cancellationToken = default);
}