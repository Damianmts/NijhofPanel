namespace NijhofPanel.Views;

using ViewModels;
using Services;

public partial class ElectricalPageView
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