namespace BranchActualizer.Input.Menu;

public class MenuResult
{
    public int OptionNumber { get; set; }

    public Option? ChosenOption { get; set; }

    public IList<Option>? AllOptions { get; set; }

    public bool IsBack => ChosenOption?.Id.Equals(IMenu.BackButtonId) is true;
}