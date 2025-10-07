namespace NijhofPanel.Views;

using System;
using System.Windows;
using System.ComponentModel;
using Services;
using UI.Themes;
using ViewModels;

public partial class MainUserControlView
{
    private MainUserControlViewModel _mainVm = null!;

    public MainUserControlView(MainUserControlViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        WarningService.Instance.Initialize(vm);
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
    
    private void CloseWarningButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainUserControlViewModel viewModel) viewModel.HideWarning();
    }

    private void MainVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainUserControlViewModel.IsDarkMode))
        {
            ThemeManager.UpdateTheme(_mainVm.IsDarkMode, this);
            if (NavigationFrame.Content is FrameworkElement activePage)
                ThemeManager.UpdateTheme(_mainVm.IsDarkMode, activePage);
        }
    }
}