using BranchActualizer.Input.Menu;

namespace BranchActualizer;

public class BranchActualizerProgram
{
    private readonly IMenuFactory _menuFactory;

    private readonly IBranchActualizerFactory _branchActualizerFactory;

    private const string SearchBranchesId = "search_branches";

    private const string SettingsId = "settings";

    public BranchActualizerProgram(IMenuFactory menuFactory, IBranchActualizerFactory branchActualizerFactory)
    {
        _menuFactory = menuFactory;

        _branchActualizerFactory = branchActualizerFactory;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var mainMenuResult = await ShowMainMenuAsync(cancellationToken);
            if (mainMenuResult.ChosenOption?.Id.Equals(SearchBranchesId) is true)
            {
                await HandleSearchBranchesMenu(cancellationToken);
            }
        }
    }

    private async Task HandleSearchBranchesMenu(CancellationToken cancellationToken = default)
    {
        var actualizer = await _branchActualizerFactory.BuildBranchActualizerAsync(cancellationToken);
        var searchMenuResult = await ShowSearchBranchesMenu(actualizer.Branches, cancellationToken);
        if (searchMenuResult.IsBack)
            return;

        await actualizer.ExcludeBranchesAsync(
            actualizer.Branches.Where(b =>
                searchMenuResult.AllOptions?.FirstOrDefault(o => 
                    o.Id.Equals(b.BranchName + b.RepositoryName))?.IsMarked is false),
            cancellationToken);

        var actualizeResult = await actualizer.ActualizeAsync(cancellationToken);
    }

    private async Task<MenuResult> ShowSearchBranchesMenu(IEnumerable<IBranchInfo> branches, CancellationToken cancellationToken = default)
    {
        var menu = await _menuFactory.BuildMenu(new MenuSettings()
        {
            Header = "Choose branch to actualize",
            HasBackButton = true,
            HasOkButton = true,
            Options = branches
                .Select(x => new Option($"{x.BranchName} ({x.RepositoryName})", x.BranchName + x.RepositoryName, true)).ToList()
        });

        return await menu.ShowAsync(cancellationToken);
    }

    private async Task<MenuResult> ShowMainMenuAsync(CancellationToken cancellationToken = default)
    {
        var menu = await _menuFactory.BuildMenu(new MenuSettings()
        {
            Options = new List<Option>()
            {
                new("Search for non-updated branches", SearchBranchesId),
                new("Settings", SettingsId)
            }
        });

        return await menu.ShowAsync(cancellationToken);
    }
}