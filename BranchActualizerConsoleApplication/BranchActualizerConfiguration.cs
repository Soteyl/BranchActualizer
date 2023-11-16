using BranchActualizer.Input;

internal class BranchActualizerConfiguration
{
    public string? BitBucketUsername { get; set; }

    public string? BitBucketAppPassword { get; set; }

    public string? TriggerChannelName { get; set; }

    public string? MessageChannelName { get; set; }
    
    public string? WorkspaceSlugOrUuid { get; set; }

    public string[] ProjectNames { get; set; }

    // Key is repository
    public Dictionary<string, List<string>>? ExcludedBranches { get; set; }
    
    public string? JiraEmail { get; set; }
    
    public string? JiraApiToken { get; set; }
    
    public List<User>? Users { get; set; }
    
    public string? JiraUrl { get; set; }
    
    public string? BitbucketUrl { get; set; }
    
    public string? SlackApiToken { get; set; }
    
    public string? SlackAppLevelToken { get; set; }
    
    public string? Project { get; set; }
    
    public string? ActualizeSince { get; set; }
}