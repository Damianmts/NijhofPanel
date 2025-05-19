using System.Windows.Controls;
using NijhofPanel.ViewModels;
using NijhofPanel.Services;
using System;
using System.Windows;
using NijhofPanel.Views;

namespace NijhofPanel.Views;

public partial class MainUserControlView : UserControl
{
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
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij openen venster: {ex.Message}");
            }
        }
    }
}