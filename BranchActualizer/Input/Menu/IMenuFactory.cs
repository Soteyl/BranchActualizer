namespace BranchActualizer.Input.Menu;

public interface IMenuFactory
{
    Task<IMenu> BuildMenu(MenuSettings settings);
}