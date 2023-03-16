using BranchActualizer.Repositories;

namespace BranchActualizer;

public class BitBucketBranchActualizerSettings
{
    public string ProjectUuid { get; set; }
    
    public string WorkspaceSlugOrUuid { get; set; }
    
    public IEnumerable<RepositoryInfo> Repositories { get; set; }

    public BitBucketBranchActualizerSettings()
    { }

    public BitBucketBranchActualizerSettings(BitBucketBranchActualizerSettings settings)
    {
        ProjectUuid = settings.ProjectUuid;
        WorkspaceSlugOrUuid = settings.WorkspaceSlugOrUuid;
        Repositories = settings.Repositories;
    }
}