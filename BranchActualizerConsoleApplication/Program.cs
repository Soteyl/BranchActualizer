using Atlassian.Jira;
using BranchActualizer;
using BranchActualizer.Branches;
using BranchActualizer.Branches.MergeResolver;
using BranchActualizer.Repositories;
using BranchActualizer.Slack;
using BranchActualizer.Slack.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpBucket.V2;
using SlackNet;

class Program
{
    private static readonly string ConfigRelativePath = Path.Combine("BranchActualizer", "config.json");

    private static SharpBucketV2 _bucket;

    private static Jira _jira;

    public static async Task Main(string[] args)
    {
        CancellationToken cancellationToken = default;

        var services = new ServiceCollection();

        await ConfigureServices(services, cancellationToken);

        Console.WriteLine("Program started");

        while (true)
        {
            await Task.Delay(int.MaxValue, cancellationToken);
        }
    }

    private static async Task ConfigureServices(ServiceCollection services, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Get configuration...");
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(ConfigRelativePath, optional: false, reloadOnChange: true)
            .Build();
        
        var config = configuration.Get<BranchActualizerConfiguration>();
        
        _jira = Jira.CreateRestClient(config!.JiraUrl, config.JiraEmail, config.JiraApiToken);
        services.AddSingleton(_jira);
        
        var mergeSolver = new JiraUsersIssuesBranchMergeSolver(_jira, config.Users?.Select(x => x.JiraId)!, config.Project, config.ActualizeSince);
        mergeSolver.With(new ExcludedBranchesMergeResolver(config.ExcludedBranches?
            .SelectMany(x => x.Value.Select(b => new BranchInfo()
            {
                Name = b,
                Repository = x.Key
            })) ?? new List<BranchInfo>()));
        services.AddSingleton<IBranchMergeSolver>(mergeSolver);
        services.AddSingleton<IBranchAuthorResolver>(mergeSolver);
        
        _bucket = new SharpBucketV2(config.BitbucketUrl!);
        _bucket.BasicAuthentication(
            config.BitBucketUsername, config.BitBucketAppPassword);
        services.AddSingleton(_bucket);

        var builder = new SlackServiceBuilder()
            .UseApiToken(config.SlackApiToken)
            .UseAppLevelToken(config.SlackAppLevelToken);
        services.AddSingleton(builder.GetApiClient());

        services.AddSingleton<IActualRepositoriesContainer>(new BitBucketActualRepositoriesCacheContainer(_bucket,
            TimeSpan.FromHours(1), config.WorkspaceSlugOrUuid!, config.ProjectUuid!));

        services.AddSingleton(new BitBucketBranchActualizerSettings()
        {
            ProjectUuid = config.ProjectUuid!,
            WorkspaceSlugOrUuid = config.WorkspaceSlugOrUuid!
        });
        services.AddSingleton(new SlackBranchActualizerSettings()
        {
            Users = config.Users,
            ResultMessageChannel = config.MessageChannelName
        });
        
        services.AddSingleton<IBranchActualizerFactory, BitbucketBranchActualizerFactory>();

        services.AddSingleton<SlackBranchActualizer>();

        var provider = services.BuildServiceProvider();
        
        builder.RegisterEventHandler(x => new ExecuteActualizingOnMessageHandler(
                config.TriggerChannelName!,
                x.ServiceProvider.GetApiClient(),
                provider.GetRequiredService<SlackBranchActualizer>()
            ))
            .RegisterEventHandler(x => new ActualizeOnReactionHandler(config.MessageChannelName!, 
                provider.GetRequiredService<SlackBranchActualizer>(), x.ServiceProvider.GetApiClient()));
        
        var socketClient = builder.GetSocketModeClient();
        await socketClient.Connect();
    }
}