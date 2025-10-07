namespace NijhofPanel.Services;

using Autodesk.Revit.UI;
using Views;

public interface IWindowService
{
    void ToggleWindow(MainUserControlView userControl, UIApplication uiApp);
    DockablePane? GetDockablePane(UIApplication uiApp);
    void UpdateTheme(bool isDarkMode);
}