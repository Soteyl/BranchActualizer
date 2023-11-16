using BranchActualizer.Repositories;

namespace BranchActualizer;

public class BitBucketBranchActualizerSettings
{
    public string[] ProjectNames { get; set; }
    
    public string WorkspaceSlugOrUuid { get; set; }
    
    public IEnumerable<RepositoryInfo> Repositories { get; set; }

    public BitBucketBranchActualizerSettings()
    { }

    public BitBucketBranchActualizerSettings(BitBucketBranchActualizerSettings settings)
    {
        ProjectNames = settings.ProjectNames;
        WorkspaceSlugOrUuid = settings.WorkspaceSlugOrUuid;
        Repositories = settings.Repositories;
    }
}