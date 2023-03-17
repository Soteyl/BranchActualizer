using BranchActualizer.Input;

namespace BranchActualizer.Slack;

public class SlackBranchActualizerSettings
{
    public string? ResultMessageChannel { get; set; }
    
    public IEnumerable<User>? Users { get; set; }
}