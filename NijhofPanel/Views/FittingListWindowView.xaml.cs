using System.Windows;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class FittingListWindowView : Window
{
    public FittingListWindowView()
    {
        InitializeComponent();
        DataContext = new FittingListWindowViewModel();
    }
}