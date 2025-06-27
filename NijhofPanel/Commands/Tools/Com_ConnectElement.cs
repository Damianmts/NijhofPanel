using System;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace NijhofPanel.Commands.Tools;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class Com_ConnectElement : IExternalEventHandler
{
    public void Execute(UIApplication uiApp)
    {
        var uidoc = uiApp.ActiveUIDocument;
        var doc = uidoc.Document;

        try
        {
            var success = ConnectElements(uidoc, doc);
            // je kunt hier eventueel een notificatie geven bij success/failure
        }
        catch (OperationCanceledException)
        {
            // gebruiker annuleerde de selectie
        }
    }

    public string GetName()
    {
        return "Com_ConnectElement";
    }

    private bool ConnectElements(UIDocument uidoc, Document doc)
    {
        // selectie van element om te verplaatsen
        var movedReference = uidoc.Selection
            .PickObject(ObjectType.Element, new NoInsulationFilter(),
                "Selecteer het element wat je wilt verbinden");
        var movedElement = doc.GetElement(movedReference);
        var movedPoint = movedReference.GlobalPoint;

        // selectie van doel-element
        var targetReference = uidoc.Selection
            .PickObject(ObjectType.Element, new NoInsulationFilter(),
                "Selecteer het element waarmee je wilt verbinden");
        var targetElement = doc.GetElement(targetReference);
        var targetPoint = targetReference.GlobalPoint;

        if (targetElement.Id == movedElement.Id)
        {
            TaskDialog.Show("Foutmelding",
                "Oeps, het lijkt erop dat je hetzelfde element hebt geselecteerd.");
            return false;
        }

        var movedConnector = GetClosestConnector(movedElement, movedPoint);
        var targetConnector = GetClosestConnector(targetElement, targetPoint);

        if (movedConnector == null || targetConnector == null)
        {
            TaskDialog.Show("Foutmelding",
                "Het lijkt erop dat het geselecteerde element geen ongebruikte connector heeft.");
            return false;
        }

        if (movedConnector.Domain != targetConnector.Domain)
        {
            TaskDialog.Show("Foutmelding",
                "Je hebt 2 elementen van verschillende systemen geselecteerd.");
            return false;
        }

        // roteer indien nodig
        var movedDir = movedConnector.CoordinateSystem.BasisZ;
        var targetDir = targetConnector.CoordinateSystem.BasisZ;
        var angle = movedDir.AngleTo(targetDir);

        if (Math.Abs(angle - Math.PI) > 1e-6)
        {
            var axisDir = angle == 0
                ? movedConnector.CoordinateSystem.BasisY
                : movedDir.CrossProduct(targetDir);

            var axis = Line.CreateBound(movedPoint, movedPoint + axisDir);
            using (var t = new Transaction(doc, "Draai Element"))
            {
                t.Start();
                movedElement.Location.Rotate(axis, angle - Math.PI);
                t.Commit();
            }
        }

        // verplaats en verbind
        using (var t = new Transaction(doc, "Verplaats En Verbind Elementen"))
        {
            t.Start();
            ((LocationPoint)movedElement.Location)
                .Move(targetConnector.Origin - movedConnector.Origin);
            movedConnector.ConnectTo(targetConnector);
            t.Commit();
        }

        return true;
    }

    private Connector GetClosestConnector(Element element, XYZ point)
    {
        ConnectorSet connectors = null;
        switch (element)
        {
            case FamilyInstance fi:
                connectors = fi.MEPModel?.ConnectorManager?.Connectors;
                break;
            case Pipe pipe:
                connectors = pipe.ConnectorManager?.Connectors;
                break;
            case Duct duct:
                connectors = duct.ConnectorManager?.Connectors;
                break;
        }

        if (connectors == null) return null;

        Connector closest = null;
        var minDist = double.MaxValue;
        foreach (Connector c in connectors)
        {
            if (c.IsConnected) continue;
            var d = c.Origin.DistanceTo(point);
            if (d < minDist)
            {
                minDist = d;
                closest = c;
            }
        }

        return closest;
    }

    private class NoInsulationFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return !(elem is InsulationLiningBase) &&
                   (elem is FamilyInstance || elem is Pipe || elem is Duct);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}