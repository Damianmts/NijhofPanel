namespace NijhofPanel.Views;

using ViewModels;

public partial class MainWindowView
{
    public MainWindowView()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}