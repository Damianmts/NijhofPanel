namespace NijhofPanel.Services;

using ViewModels;

public class WarningService
{
    private static WarningService? _instance;
    public static WarningService Instance => _instance ??= new WarningService();

    private MainUserControlViewModel? _mainViewModel;

    public void Initialize(MainUserControlViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void ShowWarning(string message)
    {
        _mainViewModel?.ShowWarning(message);
    }

    public void HideWarning()
    {
        _mainViewModel?.HideWarning();
    }
}
