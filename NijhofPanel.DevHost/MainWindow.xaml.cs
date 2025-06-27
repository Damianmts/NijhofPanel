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

        // 1) Maak de view model-factory aan
        var viewModelFactory = new NijhofPanel.Services.ViewModelFactory();

        // 2) Haal de hoofd ViewModel op
        var vm = viewModelFactory.CreateMainViewModel();

        // 3) Instantieer de hoofd UserControl met ViewModel
        var mainUserControl = new MainUserControlView(vm);

        // 4) Toon alles in de MainWindowView
        var window = new MainWindowView
        {
            Title = "NijhofPanel",
            Width = 350,
            Height = 750
        };

        window.MainContent.Content = mainUserControl;

        Application.Current.MainWindow = window;
        window.Show();

        // 5) Sluit de DevHost
        Close();
    }
}