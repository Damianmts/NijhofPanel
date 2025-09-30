namespace NijhofPanel.Views;

using ViewModels;

public partial class FittingListWindowView
{
    public FittingListWindowView()
    {
        InitializeComponent();
        DataContext = new FittingListWindowViewModel();
    }
}