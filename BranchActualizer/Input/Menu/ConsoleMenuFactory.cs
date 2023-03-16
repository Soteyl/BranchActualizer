using BranchActualizer.Input.Menu.Console;

namespace BranchActualizer.Input.Menu;

public class ConsoleMenuFactory: IMenuFactory
{
    public Task<IMenu> BuildMenu(MenuSettings settings)
    {
        return Task.FromResult<IMenu>(new ConsoleMenu(settings));
    }
}