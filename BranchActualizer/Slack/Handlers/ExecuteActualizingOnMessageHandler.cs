﻿using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BranchActualizer.Slack.Handlers;

public class ExecuteActualizingOnMessageHandler : IEventHandler<MessageEvent>
{
    private string? __botUserId = null;

    private readonly string _actualizeTriggerChannel;

    private readonly ISlackApiClient _slack;
    
    private readonly SlackBranchActualizer _actualizer;
    
    private readonly ILogger _logger;

    public ExecuteActualizingOnMessageHandler(string actualizeTriggerChannel, ISlackApiClient slack, SlackBranchActualizer actualizer,
        ILogger<ExecuteActualizingOnMessageHandler> logger)
    {
        _actualizeTriggerChannel = actualizeTriggerChannel;
        _slack = slack;
        _actualizer = actualizer;
        _logger = logger;
    }

    public async Task Handle(MessageEvent slackEvent)
    {
        try
        {
            _logger.Log(LogLevel.Information, $"Message received in channel {slackEvent.Channel}. Actualizing...");
            if ((await _slack.Conversations.Info(slackEvent.Channel)).Id.Equals(_actualizeTriggerChannel) &&
                slackEvent.User?.Equals(await GetBotId()) is false)
            {
                await _actualizer.ActualizeAsync(slackEvent.Text);
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