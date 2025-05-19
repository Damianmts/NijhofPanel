using System.Windows;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class LibraryWindowView : Window
{
    public LibraryWindowView()
    {
        InitializeComponent();
        DataContext = new LibraryWindowViewModel();
    }
}