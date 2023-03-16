namespace BranchActualizer;

public interface IBranchInfo
{
    string Author { get; }
    
    string RepositoryName { get; }
    
    string BranchName { get; }
}