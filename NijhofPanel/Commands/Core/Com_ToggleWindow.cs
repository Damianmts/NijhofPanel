namespace NijhofPanel.Commands.Core;

using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Electrical;
using NijhofPanel.Helpers.Core;
using NijhofPanel.Helpers.Electrical;
using Views;
using ViewModels;
using Services;

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
            var tagGroepnummerHandler = new Com_TagGroepnummer();
            var tagGroepnummerEvent = ExternalEvent.Create(tagGroepnummerHandler);
            var tagSwitchcodeHandler = new Com_TagSwitchcode();
            var tagSwitchcodeEvent = ExternalEvent.Create(tagSwitchcodeHandler);

            // Maak & configureer de services
            var navigationService = new NavigationService();
            var windowService = new WindowService();

            // Instantieer de Main ViewModel met alle sub-VM's
            var mainVm = new MainUserControlViewModel(navigationService, windowService)
            {
                ElectricalVm = new ElectricalPageViewModel(familyHandler, familyEvent, 
                    tagGroepnummerEvent, tagSwitchcodeEvent),
                ToolsVm      = new ToolsPageViewModel(),
                PrefabVm     = new PrefabWindowViewModel(prefabHandler, prefabEvent)
            };
            navigationService.SetMainViewModel(mainVm);

            // Maak de view met de VM (constructor injectie!)
            var mainView = new MainUserControlView(mainVm);

            // Doe de toggle
            windowService.ToggleWindow(mainView, uiApp);

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}