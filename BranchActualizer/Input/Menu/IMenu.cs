namespace BranchActualizer.Input.Menu;

public interface IMenu
{
    public const string BackButtonId = "back";

    public const string OkButtonId = "ok";

    Task<MenuResult> ShowAsync(CancellationToken token = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}