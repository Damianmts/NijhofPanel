using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using NijhofPanel.Views;
using NijhofPanel.ViewModels;

namespace NijhofPanel.DevHost;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 1) Instantieer je UserControl
        var mainUserControl = new MainUserControlView();

        // 2) Instantieer je NavigationService
        var navService = new NijhofPanel.Services.NavigationService();

        // 3) Instantieer je ViewModel
        var vm = new MainUserControlViewModel(navService);

        // 4) Stel de DataContext in
        mainUserControl.DataContext = vm;

        // 5) Stel de host in voor navigatie
        var host = mainUserControl.FindName("MainContent") as ContentControl;
        navService.SetHost(host);

        // 6) Toon het in een window
        var window = new Window
        {
            Content = mainUserControl,
            Title = "NijhofPanel",
            Width = 800,
            Height = 600
        };

        Application.Current.MainWindow = window;
        window.Show();

        // 7) Sluit de DevHost
        Close();
    }
}