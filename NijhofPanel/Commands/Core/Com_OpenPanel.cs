using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace NijhofPanel.Commands.Core;

[Transaction(TransactionMode.Manual)]
public class Com_OpenPanel : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiApp = commandData.Application;
            var dpId = new DockablePaneId(new Guid("e54d1236-371d-4b8b-9c93-30c9508f2fb9"));

            // Get the existing panel and open it
            var pane = uiApp.GetDockablePane(dpId);
            pane.Show();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = "Er is een fout opgetreden bij het openen van het panel: " + ex.Message;
            return Result.Failed;
        }
    }
}