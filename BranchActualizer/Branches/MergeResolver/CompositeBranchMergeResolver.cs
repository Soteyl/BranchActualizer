using System.Text;

namespace BranchActualizer.Branches;

public abstract class CompositeBranchMergeResolver: ICompositeBranchMergeSolver
{
    private List<IBranchMergeSolver> _other = new();
    
    public async Task<bool> ShouldMergeAsync(BranchInfo request, CancellationToken cancellationToken = default)
    {
        var tasks = _other.Select(x => x.ShouldMergeAsync(request, cancellationToken)).ToList();
        tasks.Add(ShouldMerge(request, cancellationToken));
        await Task.WhenAll(tasks);
        return tasks.All(x => x.Result);
    }

    public async Task<string> ToFilterAsync(FilterInfo info, CancellationToken cancellationToken = default)
    {
        var tasks = _other.Select(x => x.ToFilterAsync(info, cancellationToken)).ToList();
        tasks.Add(ToFilter(info, cancellationToken));

        await Task.WhenAll(tasks);

        return string.Join(" AND ", tasks.Where(x => !string.IsNullOrWhiteSpace(x.Result)).Select(x => $"({x.Result})"));
    }

    public virtual Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public ICompositeBranchMergeSolver With(IBranchMergeSolver other)
    {
        _other.Add(other);
        return this;
    }

    protected abstract Task<bool> ShouldMerge(BranchInfo request, CancellationToken cancellationToken = default);

    protected abstract Task<string> ToFilter(FilterInfo info, CancellationToken cancellationToken = default);
}