using System.Text;
using BranchActualizer.Repositories;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.WebApi;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using User = BranchActualizer.Input.User;

namespace BranchActualizer.Slack;

public class SlackBranchActualizer
{
    private string? __botUserId = null;
    
    private readonly string _resultMessageChannel;

    private readonly List<User>? _users;

    private readonly IActualRepositoriesContainer _repositories;

    private readonly IBranchActualizerFactory _factory;

    private readonly ISlackApiClient _slack;
    
    private readonly ILogger<SlackBranchActualizer> _logger;
    
    private readonly SemaphoreSlim _semaphore;

    public SlackBranchActualizer(SlackBranchActualizerSettings settings,
        IActualRepositoriesContainer repositories, IBranchActualizerFactory factory, ISlackApiClient slack, ILogger<SlackBranchActualizer> logger)
    {
        _resultMessageChannel = settings.ResultMessageChannel;
        _users = settings.Users?.ToList();
        _repositories = repositories;
        _factory = factory;
        _slack = slack;
        _logger = logger;
        _semaphore = new(1, 1);
    }

    public async Task ActualizeAsync(string messageTextWithRepository, CancellationToken cancellationToken = default)
    {
        var repositoryToActualize = (await _repositories.GetActualRepositoriesAsync(cancellationToken))
            .FirstOrDefault(x => messageTextWithRepository.Contains(x.Name, StringComparison.InvariantCultureIgnoreCase));

        _logger.Log(LogLevel.Information, $"Repository to actualize from message: {repositoryToActualize?.Name}");
        
        if (repositoryToActualize is null)
        {
            await _slack.Chat.PostMessage(new Message()
            {
                Text = "В повідомленні не знайдено назви репозиторію.",
                Channel = _resultMessageChannel
            });
            return;
        }

        var message = await _slack.Chat.PostMessage(new Message()
        {
            Text = $"Скоро актуалізую {repositoryToActualize?.Name}...",
            Channel = _resultMessageChannel
        });

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            try
            {
                await _slack.Chat.Delete(message.Ts, _resultMessageChannel, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            message = await _slack.Chat.PostMessage(new Message()
            {
                Text = $"Актуалізую {repositoryToActualize?.Name}...",
                Channel = _resultMessageChannel
            });
            
            var actualizer = await _factory.WithRepositories(new[] { repositoryToActualize })
                .BuildBranchActualizerAsync(cancellationToken);
            var result = await actualizer.ActualizeAsync(cancellationToken);

            _logger.Log(LogLevel.Information,
                $"Actualizing for {repositoryToActualize?.Name} finished. Deleting message...");

            try
            {
                await _slack.Chat.Delete(message.Ts, _resultMessageChannel, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _logger.Log(LogLevel.Information, $"Message deleted. Sending result message...");

            var resultMessage = GetResultMessage(result, repositoryToActualize?.Name);

            _logger.Log(LogLevel.Information, $"Result message: {resultMessage}");

            message = await _slack.Chat.PostMessage(new Message()
            {
                Text = resultMessage,
                Channel = _resultMessageChannel,
                LinkNames = true,
            });

            await _slack.Reactions.AddToMessage("repeat", _resultMessageChannel, message.Ts);
        }
        finally
        {
            _semaphore.Release();
        }
    }


    private string GetResultMessage(ActualizeBranchesResult result, string repositoryName)
    {
        var sb = new StringBuilder($"Виконана актуалізація гілок для {repositoryName}.\n\n");

        if (result.Conflicts.Any())
        {
            sb.AppendLine(":warning:Конфлікти:");
            foreach (var conflict in result.Conflicts)
            {
                sb.AppendLine(
                    $"*{conflict.BranchName} ({conflict.RepositoryName})* <@{_users?.FirstOrDefault(x => x.JiraId?.Equals(conflict.AuthorId) is true)?.SlackId ?? "channel"}>");
            }
        }

        if (result.CannotActualize.Any())
        {
            sb.AppendLine("\nНе вдалося актуалізувати:");
            foreach (var cannotActualize in result.CannotActualize)
            {
                sb.AppendLine(
                    $"{cannotActualize.BranchName} ({cannotActualize.RepositoryName}) - {cannotActualize.Reason} <@{_users?.FirstOrDefault(x => x.JiraId?.Equals(cannotActualize.AuthorId) is true)?.SlackId ?? "channel"}>");
            }
        }

        if (result.SuccessMerges.Any())
        {
            sb.AppendLine("\nУспішні актуалізації:");
            foreach (var merge in result.SuccessMerges)
            {
                sb.AppendLine($"{merge.BranchName} ({merge.RepositoryName})");
            }
        }

        return sb.ToString();
    }
}