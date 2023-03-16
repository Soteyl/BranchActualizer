namespace BranchActualizer.Input.Menu;

public class Option
{
    public Option(string text, string id, bool canBeMarked = false)
    {
        Text = text;
        Id = id;
        CanBeMarked = canBeMarked;
    }

    public string Text { get; set; }
        
    public string Id { get; set; }

    public bool CanBeChosen { get; set; } = true;

    public bool CanBeMarked { get; set; }

    public bool IsMarked { get; set; }
}