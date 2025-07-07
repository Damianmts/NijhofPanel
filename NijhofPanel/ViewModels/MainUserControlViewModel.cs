namespace NijhofPanel.ViewModels;

using Autodesk.Revit.UI;
using Views;
using Services;
using UI.Themes;
using System;
using System.ComponentModel;
using System.Windows.Input;
using Visibility = System.Windows.Visibility;

public class MainUserControlViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navigationService;
    public INavigationService NavigationService => _navigationService;
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged(nameof(CurrentView));
        }
    }

    public ElectricalPageViewModel? ElectricalVm { get; set; }
    public ToolsPageViewModel? ToolsVm { get; set; }
    public PrefabWindowViewModel? PrefabVm { get; set; }
    public LibraryWindowViewModel? LibraryVm { get; set; }

    private static MainWindowView _windowInstance;
    private bool _isDarkMode;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged(nameof(IsDarkMode));
                UpdateTheme();
            }
        }
    }

    public ICommand Com_ToggleTheme { get; }

    private bool _isLoggedIn;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn != value)
            {
                _isLoggedIn = value;
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(SidebarVisibility));
            }
        }
    }

    public Visibility SidebarVisibility => IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;

    private object? _currentPage;

    public object? CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }
    }

    public ICommand LoginCommand { get; }

    public MainUserControlViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Com_ToggleTheme = new RelayCommand(ExecuteToggleTheme);
        LoginCommand = new RelayCommand(ExecuteLogin);
        IsDarkMode = false;
        IsLoggedIn = false;
        NavigateToLogin();
    }

    private void ExecuteLogin()
    {
        // Hier komt je login logica
        IsLoggedIn = true;

        // Na succesvolle login, navigeer naar de startpagina
        NavigateToStartPage();
    }

    public void NavigateToLogin()
    {
        _navigationService.NavigateTo<LogInPageView>();
    }

    public void NavigateToStartPage()
    {
        _navigationService.NavigateTo<StartPageView>();
    }

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

    private void ExecuteToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }

    private void UpdateTheme()
    {
        if (_windowInstance != null) ThemeManager.UpdateTheme(IsDarkMode, _windowInstance);
    }

    private bool _isWarningVisible;

    public bool IsWarningVisible
    {
        get => _isWarningVisible;
        set
        {
            _isWarningVisible = value;
            OnPropertyChanged(nameof(IsWarningVisible));
        }
    }

    private string _warningMessage = "";

    public string WarningMessage
    {
        get => _warningMessage;
        set
        {
            _warningMessage = value;
            OnPropertyChanged(nameof(WarningMessage));
        }
    }

    // Methode om waarschuwing te tonen
    public void ShowWarning(string message)
    {
        WarningMessage = message;
        IsWarningVisible = true;
    }

    // Methode om waarschuwing te verbergen
    public void HideWarning()
    {
        IsWarningVisible = false;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}