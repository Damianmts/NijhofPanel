namespace NijhofPanel.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using Services;
using NijhofPanel.UI.Themes;
using UI.Controls.Navigation;
using ViewModels;
using Views;

public partial class MainUserControlView : UserControl
{
    private readonly Dictionary<string, Window> _openWindows = new();
    private MainUserControlViewModel _mainVm;
    private NavButton? _activeNavButton;

    public MainUserControlView(MainUserControlViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += MainUserControlView_Loaded;
    }

    private void MainUserControlView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainUserControlViewModel vm)
        {
            _mainVm = vm;
            _mainVm.PropertyChanged += MainVm_PropertyChanged;
            ThemeManager.UpdateTheme(_mainVm.IsDarkMode, this);
            if (_mainVm.NavigationService is NavigationService navService)
            {
                navService.SetHost(NavigationFrame);
                _mainVm.NavigateToLogin();
            }
        }
        else
        {
            throw new InvalidOperationException("DataContext moet een MainUserControlViewModel zijn.");
        }
    }

    private void NavButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is NavButton navButton)
        {
            if (_activeNavButton != null && _activeNavButton != navButton)
                _activeNavButton.IsActive = false;

            navButton.IsActive = true;
            _activeNavButton = navButton;
        }
    }

    private void WindowButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is WindowButton windowButton && !string.IsNullOrEmpty(windowButton.Navlink))
        {
            // Bestaande venster-logica onveranderd
            if (_openWindows.ContainsKey(windowButton.Navlink))
            {
                var existing = _openWindows[windowButton.Navlink];
                if (existing.IsVisible)
                {
                    if (existing.WindowState == WindowState.Minimized)
                        existing.WindowState = WindowState.Normal;

                    existing.Topmost = true;
                    existing.Topmost = false;
                    existing.Activate();
                    return;
                }
                else
            {
                _openWindows.Remove(windowButton.Navlink);
            }
        }

        Window? newWindow = windowButton.Navlink switch
            {
                "LibraryWindowView" => new LibraryWindowView(),
                "PrefabWindowView" => new PrefabWindowView(),
                "FittingListWindowView" => new FittingListWindowView(),
                "SawListWindowView" => new SawListWindowView(),
                _ => null
            };

            if (newWindow != null)
            {
                newWindow.Owner = Window.GetWindow(this);
                windowButton.IsWindowOpen = true;
                _openWindows[windowButton.Navlink] = newWindow;

                newWindow.Closed += (_, _) =>
                {
                    _openWindows.Remove(windowButton.Navlink);
                    windowButton.IsWindowOpen = false;
                };

                newWindow.Show();
                ThemeManager.UpdateTheme(_mainVm.IsDarkMode, newWindow);
            }
        }
    }

    private void MainVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainUserControlViewModel.IsDarkMode))
        {
            ThemeManager.UpdateTheme(_mainVm.IsDarkMode, this);
            foreach (var window in _openWindows.Values)
                ThemeManager.UpdateTheme(_mainVm.IsDarkMode, window);
        }
    }
}
