using System.Text.Json;
using Atlassian.Jira;
using BranchActualizer;
using BranchActualizer.Branches;
using BranchActualizer.Branches.MergeResolver;
using BranchActualizer.Repositories;
using BranchActualizer.Slack;
using BranchActualizer.Slack.Handlers;
using SharpBucket.V2;
using SlackNet;
using File = System.IO.File;
using User = BranchActualizer.Input.User;

class Program
{
    private static readonly string ConfigRelativePath = Path.Combine("BranchActualizer", "config.json");

    private static SharpBucketV2 _bucket;

    private static Jira _jira;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Initializing...");
        CancellationToken cancellationToken = default;

        if (!Path.Exists(ConfigRelativePath))
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigRelativePath)!);
        if (!File.Exists(ConfigRelativePath)) 
            await File.WriteAllTextAsync(ConfigRelativePath, JsonSerializer.Serialize(new BranchActualizerConfiguration(), 
                new JsonSerializerOptions()
            {
                WriteIndented = true
            }), cancellationToken);

        var config = JsonSerializer.Deserialize<BranchActualizerConfiguration>(
            await File.ReadAllTextAsync(ConfigRelativePath, cancellationToken))!;

        _jira = Jira.CreateRestClient(config.JiraUrl, config.JiraEmail, config.JiraApiToken);

        var mergeSolver = new JiraUsersIssuesBranchMergeSolver(_jira, config.Users?.Select(x => x.JiraId), config.Project, config.ActualizeSince);
        mergeSolver.With(new ExcludedBranchesMergeResolver(config.ExcludedBranches?
            .SelectMany(x => x.Value.Select(b => new BranchInfo()
            {
                Name = b,
                Repository = x.Key
            })) ?? new List<BranchInfo>()));
        
        _bucket = new SharpBucketV2(config.BitbucketUrl!);
        _bucket.BasicAuthentication(
            config.BitBucketUsername, config.BitBucketAppPassword);

        var builder = new SlackServiceBuilder()
            .UseApiToken(config.SlackApiToken)
            .UseAppLevelToken(config.SlackAppLevelToken);
        
        var slackActualizer = new SlackBranchActualizer(config.MessageChannelName!,
            config.Users,
            new BitBucketActualRepositoriesCacheContainer(_bucket,
                TimeSpan.FromHours(1), config.WorkspaceSlugOrUuid!, config.ProjectUuid!),
            new BitbucketBranchActualizerFactory(mergeSolver, mergeSolver, _bucket, new BitBucketBranchActualizerSettings()
            {
                ProjectUuid = config.ProjectUuid!,
                WorkspaceSlugOrUuid = config.WorkspaceSlugOrUuid!
            }),
            builder.GetApiClient());

        builder.RegisterEventHandler(x => new ExecuteActualizingOnMessageHandler(
                config.TriggerChannelName!,
                x.ServiceProvider.GetApiClient(),
                slackActualizer
                ))
            .RegisterEventHandler(x => new ActualizeOnReactionHandler(config.MessageChannelName, slackActualizer, x.ServiceProvider.GetApiClient()));

        var socketClient = builder.GetSocketModeClient();
        await socketClient.Connect();

        Console.WriteLine("Program started");

        while (true)
        {
            await Task.Delay(int.MaxValue);
        }
    }
}

internal class BranchActualizerConfiguration
{
    public string? BitBucketUsername { get; set; }

    public string? BitBucketAppPassword { get; set; }

    public string? TriggerChannelName { get; set; }

    public string? MessageChannelName { get; set; }
    
    public string? WorkspaceSlugOrUuid { get; set; }

    public string? ProjectUuid { get; set; }

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