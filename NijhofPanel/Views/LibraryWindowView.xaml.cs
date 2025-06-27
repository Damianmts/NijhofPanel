using System.Windows;
using NijhofPanel.Models;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class LibraryWindowView : Window
{
    private void MainTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is LibraryWindowViewModel viewModel && e.NewValue is FileItemModel selectedItem)
            viewModel.SelectedFolder = selectedItem;
    }

    public LibraryWindowView()
    {
        InitializeComponent();
        DataContext = new LibraryWindowViewModel();
    }
}