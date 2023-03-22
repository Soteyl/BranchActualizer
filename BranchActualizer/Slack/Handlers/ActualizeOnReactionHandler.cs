using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace BranchActualizer.Slack.Handlers;

public class ActualizeOnReactionHandler : IEventHandler<ReactionAdded>
{
    private string? __botUserId = null;

    private readonly string _channel;

    private readonly SlackBranchActualizer _actualizer;
    private readonly ISlackApiClient _slack;
    private readonly ILogger _logger;

    public ActualizeOnReactionHandler(string channel, SlackBranchActualizer actualizer, ISlackApiClient slack, ILogger logger)
    {
        _channel = channel;
        _actualizer = actualizer;
        _slack = slack;
        _logger = logger;
    }

    public async Task Handle(ReactionAdded slackEvent)
    {
        try
        {
            if (slackEvent.Item is ReactionMessage reactionMessage &&
                (await _slack.Conversations.Info(reactionMessage.Channel)).Id.Equals(_channel) &&
                slackEvent.User?.Equals(await GetBotId()) is false)
            {
                _logger.Log(LogLevel.Information, $"Reaction added to message {reactionMessage.Ts} in channel {_channel}. Actualizing...");
                var history = await _slack.Conversations.History(_channel, latestTs: reactionMessage.Ts, inclusive: true, limit: 1);
                var message = history.Messages.FirstOrDefault();
                await _slack.Chat.Delete(message.Ts, _channel, true);
                await _actualizer.ActualizeAsync(message.Text);
            }
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, e.ToString());
            throw;
        }
    }

    private async Task<string> GetBotId()
    {
        if (__botUserId is not null) return __botUserId;

        __botUserId = (await _slack.Auth.Test()).UserId;

        return __botUserId;
    }
}