namespace NijhofPanel.Commands.Electrical;

using Autodesk.Revit.UI;

public class Com_CodeLijst : IExternalEventHandler
{
    public void Execute(UIApplication uiApp)
    {
        var doc = uiApp.ActiveUIDocument.Document;

        // TODO: Implementeer de codelijst functionaliteit hier
        TaskDialog.Show("CodeLijst", "CodeLijst functionaliteit wordt uitgevoerd.");
    }

    public string GetName() => "Nijhof Panel CodeLijst";
}