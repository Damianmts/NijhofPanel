namespace NijhofPanel.Views;

using ViewModels;

public partial class SettingsPageView
{
    public SettingsPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
    
    public SettingsPageView()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
}