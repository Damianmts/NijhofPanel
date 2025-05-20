using System.Windows;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class LibraryWindowView : Window
{
    private void SfTreeView_QueryNodeSize(object sender, Syncfusion.UI.Xaml.TreeView.QueryNodeSizeEventArgs e)
    {
        if (e == null) return;

        double autoFitHeight = e.GetAutoFitNodeHeight();

        double minHeight = 30;
        e.Height = Math.Max(autoFitHeight, minHeight);

        e.Handled = true;
    }
    
    public LibraryWindowView()
    {
        InitializeComponent();
        DataContext = new LibraryWindowViewModel();
    }
}