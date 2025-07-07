using System.Windows.Controls;
using NijhofPanel.ViewModels;
using NijhofPanel.Services;

namespace NijhofPanel.Views;

public partial class ToolsPageView : Page
{
    public ToolsPageView(MainUserControlViewModel? mainVm = null)
    {
        InitializeComponent();

        // Bepaal de viewmodel‐instance: geef prioriteit aan de geleverde mainVm, anders de singleton
        var vm = mainVm?.ToolsVm ?? ToolsPageViewModel.Instance;

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