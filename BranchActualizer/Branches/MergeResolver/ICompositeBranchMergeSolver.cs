namespace BranchActualizer.Branches;

public interface ICompositeBranchMergeSolver: IBranchMergeSolver
{
    ICompositeBranchMergeSolver With(IBranchMergeSolver other);
}