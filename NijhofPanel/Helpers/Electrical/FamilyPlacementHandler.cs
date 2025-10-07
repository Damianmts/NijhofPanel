namespace NijhofPanel.Helpers.Electrical;

using Autodesk.Revit.UI;
using NijhofPanel.Commands.Electrical;

public class FamilyPlacementHandler : IExternalEventHandler
{
    public string ComponentType { get; set; } = "";

    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;

        if (string.IsNullOrEmpty(ComponentType))
            return;

        var placer = new Com_ElectricalFamilyPlacer();
        var (success, message) = placer.PlaceElectricalFamily(ComponentType, uidoc);

        if (!success) TaskDialog.Show("Fout bij plaatsen", message);
    }

    public string GetName()
    {
        return nameof(FamilyPlacementHandler);
    }
}

public class SymbolPlacementHandler : IExternalEventHandler
{
    public FamilySymbol? Symbol { get; set;}

    public void Execute(UIApplication app)
    {
        if (Symbol != null) app.ActiveUIDocument.PostRequestForElementTypePlacement(Symbol);
    }

    public string GetName()
    {
        return nameof(SymbolPlacementHandler);
    }
}