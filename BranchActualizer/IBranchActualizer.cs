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
}