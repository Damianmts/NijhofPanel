using System.Windows.Controls;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class StartPageView : Page
{
    public StartPageView()
    {
        InitializeComponent();
        DataContext = new StartPageViewModel();
    }
}