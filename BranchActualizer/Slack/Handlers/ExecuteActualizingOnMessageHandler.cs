using SlackNet;
using SlackNet.Events;

namespace BranchActualizer.Slack.Handlers;

public class ExecuteActualizingOnMessageHandler : IEventHandler<MessageEvent>
{
    private string? __botUserId = null;

    private readonly string _actualizeTriggerChannel;

    private readonly ISlackApiClient _slack;
    
    private readonly SlackBranchActualizer _actualizer;

    public ExecuteActualizingOnMessageHandler(string actualizeTriggerChannel, ISlackApiClient slack, SlackBranchActualizer actualizer)
    {
        _actualizeTriggerChannel = actualizeTriggerChannel;
        _slack = slack;
        _actualizer = actualizer;
    }

    public async Task Handle(MessageEvent slackEvent)
    {
        try
        {
            if ((await _slack.Conversations.Info(slackEvent.Channel)).Id.Equals(_actualizeTriggerChannel) &&
                slackEvent.User?.Equals(await GetBotId()) is false)
            {
                await _actualizer.ActualizeAsync(slackEvent.Text);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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