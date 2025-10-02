namespace NijhofPanel.Views;

using System.Windows;

public partial class LibraryWindowView
{
    // Alleen een parameterloze constructor; geen overload met LibraryWindowViewModel
    public LibraryWindowView()
    {
        InitializeComponent();
        // DataContext wordt extern gezet (bij het openen van het venster).
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