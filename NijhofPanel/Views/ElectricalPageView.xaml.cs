using System.Windows.Controls;
using NijhofPanel.ViewModels;
using NijhofPanel.Services;

namespace NijhofPanel.Views;

public partial class ElectricalPageView : Page
{
    public ElectricalPageView(MainUserControlViewModel mainVm)
    {
        InitializeComponent();
        
        var vm = mainVm.ElectricalVm;

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