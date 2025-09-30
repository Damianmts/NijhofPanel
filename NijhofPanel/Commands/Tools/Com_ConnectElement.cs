namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

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
            ConnectElements(uidoc, doc);
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

    private void ConnectElements(UIDocument uidoc, Document doc)
    {
        Reference movedRef;
        try
        {
            movedRef = uidoc.Selection.PickObject(ObjectType.Element, new NoInsulationFilter(),
                "Selecteer het eerste element wat je wilt verbinden");
        }
        catch (OperationCanceledException)
        {
            return; // gebruiker annuleerde selectie
        }

        var movedElement = doc.GetElement(movedRef);
        var movedPoint = movedRef.GlobalPoint;

        if (movedElement == null) return;
        
        var connectedElements = GetConnectedElements(movedElement);
        List<Element> elementsToMove = new List<Element> { movedElement };

        if (connectedElements.Any())
        {
            IList<Reference> extraRefs;
            try
            {
                extraRefs = uidoc.Selection.PickObjects(ObjectType.Element, new NoInsulationFilter(),
                    "Selecteer de extra elementen die mee moeten bewegen (Finish bij klaar)");
            }
            catch (OperationCanceledException)
            {
                return; // gebruiker annuleerde selectie
            }

            foreach (var r in extraRefs)
            {
                var e = doc.GetElement(r);
                if (e != null && !elementsToMove.Contains(e))
                    elementsToMove.Add(e);
            }
        }
        
        Reference targetRef;
        try
        {
            targetRef = uidoc.Selection.PickObject(ObjectType.Element, new NoInsulationFilter(),
                "Selecteer het element waarmee je wilt verbinden");
        }
        catch (OperationCanceledException)
        {
            return; // gebruiker annuleerde selectie
        }

        var targetElement = doc.GetElement(targetRef);
        var targetPoint = targetRef.GlobalPoint;

        if (targetElement.Id == movedElement.Id)
        {
            TaskDialog.Show("Foutmelding",
                "Oeps, het lijkt erop dat je hetzelfde element hebt geselecteerd.");
            return;
        }
        
        var movedConnector = GetClosestConnector(movedElement, movedPoint);
        var targetConnector = GetClosestConnector(targetElement, targetPoint);

        if (movedConnector == null || targetConnector == null)
        {
            TaskDialog.Show("Foutmelding",
                "Het lijkt erop dat een van de elementen geen vrije connector heeft.");
            return;
        }

        if (movedConnector.Domain != targetConnector.Domain)
        {
            TaskDialog.Show("Foutmelding",
                "Je hebt 2 elementen van verschillende systemen geselecteerd.");
            return;
        }
        
        var movedDir = movedConnector.CoordinateSystem.BasisZ;
        var targetDir = targetConnector.CoordinateSystem.BasisZ;
        var angle = movedDir.AngleTo(targetDir);

        if (angle > 1e-6 && Math.Abs(angle - Math.PI) > 1e-6)
        {
            var axisDir = movedDir.CrossProduct(targetDir);
            if (axisDir.IsZeroLength())
            {
                axisDir = movedDir.IsAlmostEqualTo(XYZ.BasisZ)
                    ? XYZ.BasisX
                    : XYZ.BasisZ.CrossProduct(movedDir).Normalize();
            }

            var axis = Line.CreateBound(movedPoint, movedPoint + axisDir.Normalize() * 5);
            using (var t = new Transaction(doc, "Draai Element"))
            {
                t.Start();
                movedElement.Location.Rotate(axis, angle - Math.PI);
                t.Commit();
            }
        }
        
        var moveVec = targetConnector.Origin - movedConnector.Origin;

        using (var t = new Transaction(doc, "Verplaats stelsel en verbind"))
        {
            t.Start();

            var idsToMove = elementsToMove
                .Select(e => e.Id)
                .Distinct()
                .ToList();

            ElementTransformUtils.MoveElements(doc, idsToMove, moveVec);

            movedConnector.ConnectTo(targetConnector);

            t.Commit();
        }
    }

    private Connector? GetClosestConnector(Element element, XYZ point)
    {
        ConnectorSet? connectors = null;
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

        Connector? closest = null;
        var minDist = double.MaxValue;
        foreach (Connector? c in connectors)
        {
            if (c!.IsConnected) continue;
            var d = c.Origin.DistanceTo(point);
            if (d < minDist)
            {
                minDist = d;
                closest = c;
            }
        }

        return closest;
    }

    private List<Element> GetConnectedElements(Element element)
    {
        List<Element> result = new List<Element>();

        ConnectorSet? connectors = null;
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

        if (connectors == null) return result;

        foreach (Connector c in connectors)
        {
            if (c.IsConnected)
            {
                foreach (Connector refC in c.AllRefs)
                {
                    if (refC.Owner.Id != element.Id)
                        result.Add(refC.Owner);
                }
            }
        }

        return result;
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