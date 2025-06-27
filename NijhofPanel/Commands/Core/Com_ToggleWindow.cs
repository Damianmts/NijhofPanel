using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using NijhofPanel.Helpers;
using NijhofPanel.Views;
using NijhofPanel.ViewModels;
using NijhofPanel.Services;

namespace NijhofPanel.Commands.Core;

[Transaction(TransactionMode.Manual)]
public class Com_ToggleWindow : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiApp = commandData.Application;

            // Maak de ExternalEvent-handlers (gelijk aan wat je in OnStartup doet)
            var familyHandler   = new FamilyPlacementHandler();
            var familyEvent     = ExternalEvent.Create(familyHandler);
            var prefabHandler   = new RevitRequestHandler();
            var prefabEvent     = ExternalEvent.Create(prefabHandler);

            // Maak & configureer de NavigationService
            var navigationService = new NavigationService();

            // Instantieer de Main ViewModel met alle sub-VM’s
            var mainVm = new MainUserControlViewModel(navigationService)
            {
                ElectricalVm = new ElectricalPageViewModel(familyHandler, familyEvent),
                ToolsVm      = new ToolsPageViewModel(familyEvent),
                PrefabVm     = new PrefabWindowViewModel(prefabHandler, prefabEvent)
            };
            navigationService.SetMainViewModel(mainVm);

            // Maak de view met de VM (constructor injectie!)
            var mainView = new MainUserControlView(mainVm);

            // Doe de toggle
            mainVm.ToggleWindowMode(mainView, uiApp);

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}