using NijhofPanel.Views;
using Autodesk.Revit.UI;
using System.Windows.Input;

namespace NijhofPanel.ViewModels;

public class MainUserControlViewModel
{
    private static MainWindowView _windowInstance;
    
    public void ToggleWindowMode(MainUserControlView userControl, UIApplication uiApp)
    {
        if (_windowInstance == null)
        {
            var dockablePane = GetDockablePane(uiApp);
            if (dockablePane != null)
                dockablePane.Hide();

            _windowInstance = new MainWindowView();
            _windowInstance.MainContent.Content = userControl;
            _windowInstance.Closed += (s, e) =>
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
    
    private DockablePane GetDockablePane(UIApplication uiApp)
    {
        var paneId = new DockablePaneId(new Guid("e54d1236-371d-4b8b-9c93-30c9508f2fb9"));
        return uiApp.GetDockablePane(paneId);
    }
    
    // TODO - Finish dark mode logic
    public ICommand Com_ToggleTheme { get; }

    public MainUserControlViewModel()
    {
        Com_ToggleTheme = new RelayCommand(ExecuteToggleTheme);
    }

    private void ExecuteToggleTheme()
    {
        // Hier komt de logica voor het wisselen tussen dark en light mode
        // Bijvoorbeeld:
        //IsDarkMode = !IsDarkMode;
        //ApplyTheme();
    }

}