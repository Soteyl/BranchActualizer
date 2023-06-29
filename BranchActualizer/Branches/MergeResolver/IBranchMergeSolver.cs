namespace BranchActualizer.Branches;

public interface IBranchMergeSolver
{
    Task<bool> ShouldMergeAsync(BranchInfo request, CancellationToken cancellationToken = default);

    /// <summary>
    /// https://developer.atlassian.com/cloud/bitbucket/rest/intro#filtering
    /// </summary>
    Task<string> ToFilterAsync(FilterInfo info, CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}