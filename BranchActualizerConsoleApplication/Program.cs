using Atlassian.Jira;
using BranchActualizer;
using BranchActualizer.Branches;
using BranchActualizer.Branches.MergeResolver;
using BranchActualizer.Repositories;
using BranchActualizer.Slack;
using BranchActualizer.Slack.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
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

        //use serilog in di
        var provider = services.BuildServiceProvider();

        Log.Information("Program started...");
        
        while (true)
        {
            await Task.Delay(int.MaxValue, cancellationToken);
        }
    }

    private static async Task ConfigureServices(ServiceCollection services, CancellationToken cancellationToken = default)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console()
            .WriteTo.File($"logs/ba.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog();
        });
        
        Log.Information("Get Configuration...");
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
            })) ?? new List<BranchInfo>()))
                   .With(new ExcludeIgnoreBranchesMergeResolver());
        services.AddSingleton<IBranchMergeSolver>(mergeSolver);
        services.AddSingleton<IBranchAuthorResolver>(mergeSolver);
        
        _bucket = new SharpBucketV2(config.BitbucketUrl!);
        _bucket.BasicAuthentication(
            config.BitBucketUsername, config.BitBucketAppPassword);
        services.AddSingleton(_bucket);

        var builder = new SlackServiceBuilder()
            .UseApiToken(config.SlackApiToken)
            .UseAppLevelToken(config.SlackAppLevelToken);
        var apiClient = builder.GetApiClient();
        services.AddSingleton(apiClient);

        services.AddSingleton<IActualRepositoriesContainer>(new BitBucketActualRepositoriesCacheContainer(_bucket,
            TimeSpan.FromHours(1), config.WorkspaceSlugOrUuid!, config.ProjectNames!));

        services.AddSingleton(new BitBucketBranchActualizerSettings()
        {
            ProjectNames = config.ProjectNames,
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
                provider.GetRequiredService<SlackBranchActualizer>(),
                provider.GetRequiredService<ILogger<ExecuteActualizingOnMessageHandler>>(),
                DateTime.Now
            ))
            .RegisterEventHandler(x => new ActualizeOnReactionHandler(config.MessageChannelName!, 
                provider.GetRequiredService<SlackBranchActualizer>(), x.ServiceProvider.GetApiClient(), 
                provider.GetRequiredService<ILogger<ActualizeOnReactionHandler>>()));
        
        var socketClient = builder.GetSocketModeClient();

        services.AddSingleton(socketClient);
        
        await socketClient.Connect();
    }
}