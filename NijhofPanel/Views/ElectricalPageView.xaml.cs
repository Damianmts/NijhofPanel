using System.Windows.Controls;
using NijhofPanel.ViewModels;
using NijhofPanel.Services;

namespace NijhofPanel.Views;

public partial class ElectricalPageView : Page
{
    public ElectricalPageView(MainUserControlViewModel? mainVm = null)
    {
        InitializeComponent();

        // Kies eerst de ElectricalVm uit mainVm, anders de singleton
        var vm = mainVm?.ElectricalVm ?? ElectricalPageViewModel.Instance;

        if (vm != null)
        {
            DataContext = vm;
        }
        else
        {
            WarningService.Instance.ShowWarning("ElectricalPageViewModel niet geïnitialiseerd.");
        }
    }
}