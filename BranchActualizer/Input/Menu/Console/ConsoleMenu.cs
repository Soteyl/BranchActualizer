namespace BranchActualizer.Input.Menu.Console;

public class ConsoleMenu : IMenu
{
    private readonly List<Option> _options;

    private int __currentOption = 0;

    private readonly string _header;

    private int _firstOptionLineNumber = 0;

    private int CurrentOption
    {
        get => __currentOption;
        set => __currentOption = Math.Clamp(value, 0, _options.Count - 1);
    }
    public ConsoleMenu(MenuSettings settings)
    {
        _options = settings.Options.ToList();
        if (settings.HasOkButton)
            _options.Add(new Option("OK", IMenu.OkButtonId));
        if (settings.HasBackButton)
            _options.Add(new Option("<- Back", IMenu.BackButtonId));
        _header = settings.Header;
        _firstOptionLineNumber = _header?.Split('\n').Length ?? 0;
    }

    public async Task<MenuResult> ShowAsync(CancellationToken token = default)
    {
        DrawMenu();
        while (!token.IsCancellationRequested && await WaitForKey(token) is var key and not null)
        {
            if (HandleKeyForFinish(key.Value))
            {
                return new MenuResult()
                {
                    AllOptions = _options,
                    ChosenOption = _options[CurrentOption],
                    OptionNumber = CurrentOption
                };
            }
        }

        await ClearAsync(token);
        return await Task.FromCanceled<MenuResult>(token);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        System.Console.Clear();
        return Task.CompletedTask;
    }

    private async Task<ConsoleKeyInfo?> WaitForKey(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (System.Console.KeyAvailable)
                return System.Console.ReadKey();
            await Task.Delay(100, cancellationToken);
        }

        return null;
    }

    private void DrawMenu()
    {
        System.Console.Clear();
        if (_options is not { Count: > 0 })
        {
            System.Console.WriteLine("There is no options at this menu.");
            return;
        }

        if (!string.IsNullOrEmpty(_header))
        {
            System.Console.SetCursorPosition(0, 0);
            System.Console.Write(_header);
        }
        
        for (int i = 0; i < _options.Count; i++)
        {
            RedrawOption(i);
        }
    }

    private void RedrawOption(int optionNumber)
    {
        System.Console.SetCursorPosition(0, optionNumber + _firstOptionLineNumber);
        System.Console.Write(new string(' ', System.Console.WindowWidth));
        System.Console.SetCursorPosition(0, optionNumber + _firstOptionLineNumber);
        string text = _options[optionNumber].Text;
        if (_options[optionNumber].IsMarked)
            text += " (*)";
        if (optionNumber == CurrentOption)
            Write(text, ConsoleColor.DarkGray);
        else Write(text);
        System.Console.SetCursorPosition(0, System.Console.WindowHeight - 1);
    }

    private bool HandleKeyForFinish(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                CurrentOption--;
                RedrawOption(CurrentOption);
                RedrawOption(CurrentOption + 1);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                CurrentOption++;
                RedrawOption(CurrentOption);
                RedrawOption(CurrentOption - 1);
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.Z:
                var current = _options[CurrentOption];
                if (current.CanBeMarked)
                {
                    current.IsMarked = !current.IsMarked;
                    RedrawOption(CurrentOption);
                    break;
                }

                return true;
        }

        return false;
    }

    private static void Write(string text, ConsoleColor backColor = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White)
    {
        var wasBack = System.Console.BackgroundColor;
        var wasFore = System.Console.ForegroundColor;
        System.Console.BackgroundColor = backColor;
        System.Console.ForegroundColor = foreground;
        System.Console.Write(text);
        System.Console.BackgroundColor = wasBack;
        System.Console.ForegroundColor = wasFore;
    }
}