using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace NijhofPanel.Commands.Tools;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class Com_UpdateHWALength : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;
        View activeView = uidoc.ActiveView;
        
        int modifiedPipesCount = 0;
        bool allPipesAlready800 = true;

        using (Transaction tx = new Transaction(doc, "Update Pipe Length"))
        {
            tx.Start();

            FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id)
                .OfClass(typeof(Pipe));

            foreach (Pipe pipe in collector)
            {
                Parameter abbreviationParam = pipe.LookupParameter("System Abbreviation");
                if (abbreviationParam != null &&
                    (abbreviationParam.AsString() == "M521" || abbreviationParam.AsString() == "M5210"))
                {
                    Line pipeLine = (pipe.Location as LocationCurve)?.Curve as Line;
                    if (pipeLine != null && IsVertical(pipeLine))
                    {
                        Connector topConnector = GetTopConnector(pipe, pipeLine);
                        if (topConnector == null) continue;

                        Connector fittingConnector = GetConnectedFittingConnector(topConnector);

                        double desiredLength = 800 / 304.8; // Revit gebruikt voet
                        double currentLength = pipeLine.Length;

                        // Check of de huidige lengte al 800 mm is
                        if (Math.Abs(currentLength - desiredLength) < 1e-6)
                        {
                            continue; // De lengte is al correct
                        }

                        allPipesAlready800 = false; // Er is ten minste één pijp die nog niet 800 mm is

                        if (fittingConnector != null)
                        {
                            // Buislengte aanpassen en opnieuw verbinden met fitting
                            XYZ startPoint = pipeLine.GetEndPoint(0);
                            XYZ newEndPoint = (topConnector.Origin.Z > startPoint.Z)
                                ? new XYZ(topConnector.Origin.X, topConnector.Origin.Y, startPoint.Z + desiredLength)
                                : new XYZ(startPoint.X, startPoint.Y, startPoint.Z + desiredLength);

                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(startPoint, newEndPoint);
                            MoveAndReconnectElements(doc, topConnector, fittingConnector, newEndPoint.Z);
                        }
                        else
                        {
                            // Geen fitting aanwezig, alleen de lengte aanpassen
                            XYZ startPoint = pipeLine.GetEndPoint(0);
                            XYZ newEndPoint = new XYZ(startPoint.X, startPoint.Y, startPoint.Z + desiredLength);

                            (pipe.Location as LocationCurve).Curve = Line.CreateBound(startPoint, newEndPoint);
                        }

                        modifiedPipesCount++;
                    }
                }
            }

            tx.Commit();
        }

        if (modifiedPipesCount > 0)
        {
            TaskDialog.Show("Resultaat", $"{modifiedPipesCount} pijpen zijn aangepast naar een lengte van 800 mm.");
        }
        else if (allPipesAlready800)
        {
            TaskDialog.Show("Resultaat", "Alles is al geüpdatet naar een lengte van 800 mm.");
        }
        else
        {
            TaskDialog.Show("Resultaat", "Er zijn geen pijpen gevonden die aangepast moesten worden.");
        }
    }

    public string GetName()
    {
        return "Update HWA Length Handler";
    }
    
    private bool IsVertical(Line line)
    {
        XYZ direction = line.Direction;
        return (Math.Abs(direction.X) < 1e-9 && Math.Abs(direction.Y) < 1e-9 &&
                Math.Abs(Math.Abs(direction.Z) - 1) < 1e-9);
    }

    private Connector GetTopConnector(Pipe pipe, Line pipeLine)
    {
        XYZ startPoint = pipeLine.GetEndPoint(0);
        XYZ endPoint = pipeLine.GetEndPoint(1);
        ConnectorSet connectors = pipe.ConnectorManager.Connectors;

        foreach (Connector connector in connectors)
        {
            if ((endPoint.Z > startPoint.Z && connector.Origin.IsAlmostEqualTo(endPoint)) ||
                (startPoint.Z > endPoint.Z && connector.Origin.IsAlmostEqualTo(startPoint)))
            {
                return connector;
            }
        }

        return null;
    }

    private Connector GetConnectedFittingConnector(Connector pipeConnector)
    {
        foreach (Connector refConnector in pipeConnector.AllRefs)
        {
            if (refConnector.Owner is FamilyInstance fi &&
                fi.Category?.Id?.Value == (int)BuiltInCategory.OST_PipeFitting)
            {
                return refConnector;
            }
        }
        return null;
    }

    private void MoveAndReconnectElements(Document doc, Connector pipeConnector, Connector fittingConnector,
        double newZ)
    {
        double zOffset = newZ - fittingConnector.Origin.Z;

        Element fittingElement = fittingConnector.Owner;
        if (fittingElement.Location is LocationPoint locationPoint)
        {
            XYZ originalLocation = locationPoint.Point;
            XYZ newLocation = new XYZ(originalLocation.X, originalLocation.Y, originalLocation.Z + zOffset);
            locationPoint.Point = newLocation;
        }

        if (!pipeConnector.IsConnectedTo(fittingConnector))
        {
            pipeConnector.ConnectTo(fittingConnector);
        }
    }
}