using System.Windows.Controls;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class SettingsPageView : Page
{
    public SettingsPageView()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
}