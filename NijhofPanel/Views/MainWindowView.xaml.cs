using System.Windows;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class MainWindowView : Window
{
    public MainWindowView()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}