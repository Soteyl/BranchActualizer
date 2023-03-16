namespace BranchActualizer.Branches;

public interface IBranchAuthorResolver
{
    Task<string?> GetAuthorAsync(string branch, CancellationToken cancellationToken = default);
}