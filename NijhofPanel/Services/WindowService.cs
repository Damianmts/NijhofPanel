namespace NijhofPanel.Services;

using Autodesk.Revit.UI;
using Views;
using UI.Themes;
using System;

public class WindowService : IWindowService
{
    private MainWindowView? _windowInstance;

    public void ToggleWindow(MainUserControlView userControl, UIApplication uiApp)
    {
        if (_windowInstance == null)
        {
            var dockablePane = GetDockablePane(uiApp);
            dockablePane?.Hide();

            _windowInstance = new MainWindowView();
            _windowInstance.MainContent.Content = userControl;
            _windowInstance.Closed += (_, _) =>
            {
                _windowInstance = null;
                dockablePane?.Show();
            };
            _windowInstance.Show();
        }
        else
        {
            _windowInstance.Close();
            _windowInstance = null;
        }
    }

    public DockablePane? GetDockablePane(UIApplication uiApp)
    {
        var paneId = new DockablePaneId(new Guid("e54d1236-371d-4b8b-9c93-30c9508f2fb9"));
        try
        {
            return uiApp.GetDockablePane(paneId);
        }
        catch
        {
            return null;
        }
    }

    public void UpdateTheme(bool isDarkMode)
    {
        if (_windowInstance != null)
            ThemeManager.UpdateTheme(isDarkMode, _windowInstance);
    }
}