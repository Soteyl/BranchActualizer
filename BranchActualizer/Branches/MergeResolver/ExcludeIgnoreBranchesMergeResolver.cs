namespace BranchActualizer.Branches.MergeResolver;

public class ExcludeIgnoreBranchesMergeResolver: CompositeBranchMergeResolver
{
    protected override async Task<bool> ShouldMerge(BranchInfo request, CancellationToken cancellationToken = default)
    {
        return !request.Name.Contains("ignore/");
    }

    protected override async Task<string> ToFilter(FilterInfo info, CancellationToken cancellationToken = default)
    {
        return string.Empty;
    }
}