namespace NijhofPanel.ViewModels;

using Autodesk.Revit.UI;
using Views;
using Services;
using UI.Themes;
using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.Generic;
using UI.Controls.Navigation;
using Visibility = System.Windows.Visibility;

public class MainUserControlViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    public INavigationService NavigationService => _navigationService;
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public ElectricalPageViewModel? ElectricalVm { get; set; }
    public ToolsPageViewModel? ToolsVm { get; set; }
    public PrefabWindowViewModel? PrefabVm { get; set; }
    public LibraryWindowViewModel? LibraryVm { get; set; }
    
    private bool _isDarkMode;
    private readonly Dictionary<string, Window> _openWindows = new();
    private NavButton? _activeNavButton;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();
                _windowService.UpdateTheme(IsDarkMode);
            }
        }
    }

    public ICommand Com_ToggleTheme { get; }
    public ICommand NavigateCommand { get; }
    public ICommand OpenWindowCommand { get; }

    private bool _isLoggedIn;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn != value)
            {
                _isLoggedIn = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }

    public ICommand LoginCommand { get; }

    public MainUserControlViewModel(INavigationService navigationService, IWindowService windowService)
    {
        _navigationService = navigationService;
        _windowService = windowService;
        Com_ToggleTheme = new RelayCommand(ExecuteToggleTheme);
        LoginCommand = new RelayCommand(ExecuteLogin);
        NavigateCommand = new RelayCommand<NavButton>(ExecuteNavigate);
        OpenWindowCommand = new RelayCommand<WindowButton>(ExecuteOpenWindow);
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
    
    private void ExecuteNavigate(NavButton navButton)
    {
        if (_activeNavButton != null && _activeNavButton != navButton)
            _activeNavButton.IsActive = false;

        navButton.IsActive = true;
        _activeNavButton = navButton;

        if (navButton.ViewType != null)
        {
            var method = typeof(INavigationService).GetMethod(nameof(INavigationService.NavigateTo))
                ?.MakeGenericMethod(navButton.ViewType);
            method?.Invoke(_navigationService, null);
        }
    }

    private void ExecuteOpenWindow(WindowButton windowButton)
    {
        if (string.IsNullOrEmpty(windowButton.Navlink)) return;

        if (_openWindows.TryGetValue(windowButton.Navlink, out var existing))
        {
            if (existing.IsVisible)
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Topmost = true;
                existing.Topmost = false;
                existing.Activate();
                return;
            }

            _openWindows.Remove(windowButton.Navlink);
        }

        Window? newWindow = windowButton.Navlink switch
        {
            "LibraryWindowView" => new LibraryWindowView(),
            "PrefabWindowView" => PrefabVm != null ? new PrefabWindowView(PrefabVm) : null,
            "FittingListWindowView" => new FittingListWindowView(),
            "SawListWindowView" => new SawListWindowView(),
            _ => null
        };

        if (newWindow != null)
        {
            newWindow.Owner = Application.Current.MainWindow;
            windowButton.IsWindowOpen = true;
            _openWindows[windowButton.Navlink] = newWindow;

            newWindow.Closed += (_, _) =>
            {
                _openWindows.Remove(windowButton.Navlink);
                windowButton.IsWindowOpen = false;
            };

            newWindow.Show();
            ThemeManager.UpdateTheme(IsDarkMode, newWindow);
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
    
    private bool _isWarningVisible;

    public bool IsWarningVisible
    {
        get => _isWarningVisible;
        set
        {
            _isWarningVisible = value;
            OnPropertyChanged();
        }
    }

    private string _warningMessage = "";

    public string WarningMessage
    {
        get => _warningMessage;
        set
        {
            _warningMessage = value;
            OnPropertyChanged();
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
}