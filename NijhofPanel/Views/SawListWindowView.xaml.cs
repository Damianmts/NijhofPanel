namespace NijhofPanel.Views;

using ViewModels;

public partial class SawListWindowView
{
    public SawListWindowView()
    {
        InitializeComponent();
        DataContext = new SawListWindowViewModel();
    }
}