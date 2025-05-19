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
    // Lijst om geopende vensters bij te houden
    private readonly Dictionary<string, Window> _openWindows = new Dictionary<string, Window>();

    public MainUserControlView(MainUserControlViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Select StartPageView at startup
        sidebar.SelectedIndex = 0;
    }
    
    private void sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sidebar.SelectedItem is Srv_NavButton navButton)
        {
            navframe.Navigate(navButton.Navlink);
        }
        else if (sidebar.SelectedItem is Srv_WindowButton windowButton && !string.IsNullOrEmpty(windowButton.Navlink))
        {
            try
            {
                // Controleer eerst of het venster al open is
                if (_openWindows.ContainsKey(windowButton.Navlink))
                {
                    // Activeer het bestaande venster
                    var existingWindow = _openWindows[windowButton.Navlink];
                    if (existingWindow.IsVisible)
                    {
                        existingWindow.Activate();
                        return;
                    }
                    else
                    {
                        // Verwijder het venster uit de dictionary als het gesloten is
                        _openWindows.Remove(windowButton.Navlink);
                    }
                }

                Window? window = null;

                switch (windowButton.Navlink)
                {
                    case "LibraryWindowView":
                        window = new LibraryWindowView();
                        break;
                    case "PrefabWindowView":
                        window = new PrefabWindowView();
                        break;
                    case "FittingListWindowView":
                        window = new FittingListWindowView();
                        break;
                    case "SawListWindowView":
                        window = new SawListWindowView();
                        break;
                }

                if (window != null)
                {
                    // Maak het venster modeless
                    window.Owner = Window.GetWindow(this);
                    
                    // Voeg het venster toe aan de dictionary
                    _openWindows[windowButton.Navlink] = window;
                    
                    // Registreer een handler voor als het venster sluit
                    window.Closed += (s, args) =>
                    {
                        _openWindows.Remove(windowButton.Navlink);
                        // Reset de selectie om de "geselecteerde" status te verwijderen
                        sidebar.SelectedIndex = -1;
                    };
                    
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij openen venster: {ex.Message}");
            }
        }
    }
    private void OnNavButtonClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Srv_NavButton navButton && navButton.Navlink != null)
        {
            try
            {
                navframe.Navigate(navButton.Navlink);
                sidebar.SelectedItem = navButton;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij navigatie (klik): {ex.Message}");
            }
        }
    }
}