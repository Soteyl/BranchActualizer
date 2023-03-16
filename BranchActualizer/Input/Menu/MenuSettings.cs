namespace BranchActualizer.Input.Menu;

public class MenuSettings
{
    public IList<Option> Options { get; set; }

    public bool HasBackButton { get; set; } = false;

    public bool HasOkButton { get; set; } = false;
    
    public string Header { get; set; }
}