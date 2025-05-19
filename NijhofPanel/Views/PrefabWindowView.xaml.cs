using System.Windows;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class PrefabWindowView : Window
{
    public PrefabWindowView()
    {
        InitializeComponent();
        DataContext = new PrefabWindowViewModel();
    }
}