using System.Windows.Controls;
using NijhofPanel.ViewModels;
using NijhofPanel.Services;
using System;
using System.Windows;
using System.Windows.Input;
using NijhofPanel.Views;

namespace NijhofPanel.Views;

public partial class MainUserControlView : UserControl
{
    private readonly Dictionary<string, Window> _openWindows = new();
    
    private NavButtonService? _activeNavButton;

    public MainUserControlView(MainUserControlViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        Loaded += (_, _) =>
        {
            if (Sidebar.Items[0] is NavButtonService firstButton)
            {
                NavButton_Click(firstButton, null!);
            }
        };
    }

    private void NavButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is NavButtonService navButton)
        {
            if (navButton.Command != null && navButton.Command.CanExecute(null) && navButton.Navlink == null)
            {
                navButton.Command.Execute(null);
                
                Sidebar.SelectedItem = null;
                SidebarBottom.SelectedItem = null;
                return;
            }
            
            if (_activeNavButton != null && _activeNavButton != navButton)
                _activeNavButton.IsActive = false;

            navButton.IsActive = true;
            _activeNavButton = navButton;

            if (navButton.Navlink != null)
            {
                try
                {
                    Navframe.Navigate(navButton.Navlink);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij navigatie: {ex.Message}");
                }
            }

            Sidebar.SelectedItem = null;
            SidebarBottom.SelectedItem = null;
        }
    }

    private void WindowButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is WindowButtonService windowButton && !string.IsNullOrEmpty(windowButton.Navlink))
        {
            try
            {
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij openen venster: {ex.Message}");
            }
        }
    }
}
