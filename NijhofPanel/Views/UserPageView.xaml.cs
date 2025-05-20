using System.Windows.Controls;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class UserPageView : Page
{
    public UserPageView()
    {
        InitializeComponent();
        DataContext = new UserPageViewModel();
    }
}