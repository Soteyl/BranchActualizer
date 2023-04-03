using System.Text;
using BranchActualizer.Input;

namespace BranchActualizer;

public interface IBranchActualizer
{
    IEnumerable<IBranchInfo> Branches { get; }

    Task<ActualizeBranchesResult> ActualizeAsync(CancellationToken cancellationToken = default);

    Task ExcludeBranchesAsync(IEnumerable<IBranchInfo> branches, CancellationToken cancellationToken = default);
}

public class ActualizeBranchesResult
{
    public IEnumerable<PullRequestMergeConflict> Conflicts { get; set; }
    
    public IEnumerable<IBranchInfo> SuccessMerges { get; set; }
    
    public IEnumerable<CannotActualize> CannotActualize { get; set; }
}

public class CannotActualize
{
    public string BranchName { get; set; }
    
    public string RepositoryName { get; set; }
    
    public string AuthorId { get; set; }
    
    public string Reason { get; set; }
}