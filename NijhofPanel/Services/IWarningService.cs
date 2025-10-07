namespace NijhofPanel.Services;

public interface IWarningService
{
    event EventHandler<string> WarningRaised;
    event EventHandler WarningCleared;

    void ShowWarning(string message);
    void HideWarning();
}