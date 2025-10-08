namespace NijhofPanel.Views;

using System.Windows;
using ViewModels;

public partial class LibraryWindowView
{
    // Alleen een parameterloze constructor; geen overload met LibraryWindowViewModel
    public LibraryWindowView()
    {
        InitializeComponent();
        
        Loaded += (s, e) =>
        {
            if (DataContext is LibraryWindowViewModel vm)
                vm.CloseAction = Close;
        };
    }

    private void MainTreeView_SelectedItemChanged(object sender, 
        RoutedPropertyChangedEventArgs<object> e)
    {
        // Geen directe afhankelijkheid naar LibraryWindowViewModel of FileItemModel
        // Zorgt dat DevHost geen Revit-assemblies hoeft te laden.
        var dc = DataContext;
        if (dc == null) return;

        var prop = dc.GetType().GetProperty("SelectedFolder");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(dc, e.NewValue);
        }
    }
}