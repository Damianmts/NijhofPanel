namespace NijhofPanel.Views;

using System.Windows;
using Models;
using ViewModels;

public partial class LibraryWindowView
{
    public LibraryWindowView()
    {
        InitializeComponent();

        // Resolve the static handler/event you registered in RevitApplication
        DataContext = new LibraryWindowViewModel(
            RevitApplication.LibraryHandler,
            RevitApplication.LibraryEvent);
    }

    private void MainTreeView_SelectedItemChanged(object sender, 
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is LibraryWindowViewModel vm
            && e.NewValue is FileItemModel item)
        {
            vm.SelectedFolder = item;
        }
    }
}