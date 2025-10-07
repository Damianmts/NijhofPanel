namespace NijhofPanel.Commands.Tools.Shortcuts;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class Cmd_ConnectElementShortcut : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (RevitApplication.ConnectElementEvent == null)
        {
            message = "ConnectElementEvent is niet geïnitialiseerd.";
            return Result.Failed;
        }

        RevitApplication.ConnectElementEvent.Raise();
        return Result.Succeeded;
    }
}
