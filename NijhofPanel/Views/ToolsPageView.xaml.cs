namespace NijhofPanel.Views;

using ViewModels;
using Services;

public partial class ToolsPageView
{
    public ToolsPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();

        // Bepaal de viewmodel‐instance: geef prioriteit aan de geleverde mainVm, anders de singleton
        var vm = mainVm.ToolsVm;

        if (vm != null)
        {
            DataContext = vm;
        }
        else
        {
            WarningService.Instance.ShowWarning("ToolsPageViewModel niet geïnitialiseerd.");
        }
    }
}