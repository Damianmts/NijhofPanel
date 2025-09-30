namespace NijhofPanel.Views;

using ViewModels;

public partial class LogInPageView
{
    public LogInPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();
        DataContext = new UserPageViewModel(mainVm);
    }
}