namespace BranchActualizer.Branches;

public class BranchInfo
{
    public string Name { get; set; }
    
    public string Repository { get; set; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((BranchInfo)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Repository);
    }
    
    protected bool Equals(BranchInfo other)
    {
        return Name == other.Name && Repository == other.Repository;
    }
}