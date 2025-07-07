using System.Windows.Controls;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class LogInPageView : Page
{
    public LogInPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();
        DataContext = new UserPageViewModel(mainVm);
    }
}