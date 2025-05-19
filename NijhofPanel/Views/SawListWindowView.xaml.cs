using System.Windows;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class SawListWindowView : Window
{
    public SawListWindowView()
    {
        InitializeComponent();
        DataContext = new SawListWindowViewModel();
    }
}