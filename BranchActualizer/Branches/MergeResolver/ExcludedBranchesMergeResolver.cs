namespace BranchActualizer.Branches.MergeResolver;

public class ExcludedBranchesMergeResolver: CompositeBranchMergeResolver
{
    private IEnumerable<BranchInfo>? _excluded;

    public ExcludedBranchesMergeResolver(IEnumerable<BranchInfo>? excluded)
    {
        _excluded = excluded;
    }

    protected override Task<bool> ShouldMerge(BranchInfo request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_excluded?.Any(x => x.Equals(request)) is false);
    }

    protected override Task<string> ToFilter(FilterInfo info, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_excluded?.Any() is not true ? string.Empty 
            : string.Join(" AND ", _excluded!.Where(x => x.Repository.Equals(info.RepositorySlug, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => $"name != \"{x.Name}\"")));
    }
}