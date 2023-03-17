using System.Text;
using BranchActualizer.Repositories;
using SlackNet;
using SlackNet.WebApi;
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

    public SlackBranchActualizer(SlackBranchActualizerSettings settings,
        IActualRepositoriesContainer repositories, IBranchActualizerFactory factory, ISlackApiClient slack)
    {
        _resultMessageChannel = settings.ResultMessageChannel;
        _users = settings.Users?.ToList();
        _repositories = repositories;
        _factory = factory;
        _slack = slack;
    }

    public async Task ActualizeAsync(string messageTextWithRepository, CancellationToken cancellationToken = default)
    {
        var repositoryToActualize = (await _repositories.GetActualRepositoriesAsync(cancellationToken))
            .FirstOrDefault(x => messageTextWithRepository.Contains(x.Name, StringComparison.InvariantCultureIgnoreCase));

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
            Text = $"Актуалізую {repositoryToActualize?.Name}...",
            Channel = _resultMessageChannel
        });

        var actualizer = await _factory.WithRepositories(new[] { repositoryToActualize })
            .BuildBranchActualizerAsync(cancellationToken);
        var result = await actualizer.ActualizeAsync(cancellationToken);

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
            Text = GetResultMessage(result, repositoryToActualize?.Name),
            Channel = _resultMessageChannel,
            LinkNames = true,
        });

        await _slack.Reactions.AddToMessage("repeat", _resultMessageChannel, message.Ts);
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