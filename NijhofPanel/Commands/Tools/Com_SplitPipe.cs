using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NijhofPanel.Core;

namespace NijhofPanel.Commands.Tools;

public class Com_SplitPipe : IExternalEventHandler
{
    // 5000 mm in feet (Revit interne eenheid)
    private const double SegmentLengthFeet = 5000.0 / 304.8;

    public void Execute(UIApplication app)
    {
        var uiDoc = app.ActiveUIDocument ?? RevitContext.Uidoc;
        if (uiDoc == null) return;

        var doc = uiDoc.Document;

        try
        {
            // Selecteer een Pipe of Duct
            var reference = uiDoc.Selection.PickObject(
                ObjectType.Element,
                new MepCurveSelectionFilter(),
                "Selecteer een Pipe of Duct om op te splitsen");
            if (reference == null) return;

            var mepCurve = doc.GetElement(reference) as MEPCurve;
            if (mepCurve == null)
            {
                TaskDialog.Show("Split Pipe/Duct", "Geselecteerd element is geen Pipe of Duct.");
                return;
            }

            bool isPipe = mepCurve is Pipe;
            bool isDuct = mepCurve is Duct;
            if (!isPipe && !isDuct)
            {
                TaskDialog.Show("Split Pipe/Duct", "Selecteer een Pipe of Duct.");
                return;
            }

            var tg = new TransactionGroup(doc, "Split in 5000 mm met sokken");
            tg.Start();

            using (var t = new Transaction(doc, "Split en sokken plaatsen"))
            {
                t.Start();

                // Werk steeds op de 'tail' zodat elk segment ~5000mm wordt en aan het einde restlengte overblijft
                ElementId currentId = mepCurve.Id;

                while (true)
                {
                    var current = (MEPCurve)doc.GetElement(currentId);
                    if (current == null)
                        break;

                    var lc = current.Location as LocationCurve;
                    if (lc == null)
                        break;

                    var curve = lc.Curve;
                    double totalLen = curve.Length;

                    // Stop als resterende lengte <= segmentlengte
                    if (totalLen <= SegmentLengthFeet + 1e-06)
                        break;

                    // Bepaal punt op lengte SegmentLengthFeet vanaf start
                    XYZ splitPoint = PointAlongCurveByLength(curve, SegmentLengthFeet);
                    if (splitPoint == null)
                        break;

                    // BreakCurve geeft de nieuwe (tail) ElementId terug
                    ElementId newTailId;
                    if (isPipe)
                    {
                        newTailId = PlumbingUtils.BreakCurve(doc, currentId, splitPoint);
                    }
                    else
                    {
                        newTailId = MechanicalUtils.BreakCurve(doc, currentId, splitPoint);
                    }

                    doc.Regenerate();

                    // Huidige head en nieuw tail element ophalen
                    var head = (MEPCurve)doc.GetElement(currentId);
                    var tail = (MEPCurve)doc.GetElement(newTailId);

                    // Dichtstbijzijnde connectors op breuklocatie bepalen
                    var cHead = GetClosestConnector(head, splitPoint);
                    var cTail = GetClosestConnector(tail, splitPoint);

                    if (cHead != null && cTail != null)
                    {
                        // Plaats union fitting (sok) tussen de twee connectors
                        var fitting = doc.Create.NewUnionFitting(cHead, cTail);
                        doc.Regenerate();

                        // Corrigeer lengte van het "head"-segment naar exact 5000 mm
                        var headLcAfter = head.Location as LocationCurve;
                        var headCurveAfter = headLcAfter?.Curve;
                        if (headCurveAfter != null)
                        {
                            double headLenMeasured = headCurveAfter.Length;
                            double delta = SegmentLengthFeet - headLenMeasured; // in feet

                            const double tol = 1e-05; // ~0.003 mm
                            if (Math.Abs(delta) > tol)
                            {
                                // Bepaal richting langs de buis-as die de head-lengte vergroot
                                XYZ ep0 = headCurveAfter.GetEndPoint(0);
                                XYZ ep1 = headCurveAfter.GetEndPoint(1);
                                XYZ dirHead = (ep1 - ep0).Normalize();

                                // Welke endpoint is aangesloten op de fitting (dichtst bij splitPoint)?
                                bool fittingAtEp0 = ep0.DistanceTo(splitPoint) <= ep1.DistanceTo(splitPoint);
                                XYZ moveVec = (fittingAtEp0 ? -dirHead : dirHead).Multiply(delta);

                                ElementTransformUtils.MoveElement(doc, fitting.Id, moveVec);
                                doc.Regenerate();
                            }
                        }
                    }

                    // Ga verder met tail om volgende 5000 mm te knippen
                    // Kies expliciet het volgende "current" segment:
                    // Ga alleen verder met het segment dat nog langer is dan de segmentlengte.
                    // Dit voorkomt stoppen na 1 knip als BreakCurve de "head" als nieuw id teruggeeft.
                    var headLen = (head.Location as LocationCurve)?.Curve.Length ?? 0.0;
                    var tailLen = (tail.Location as LocationCurve)?.Curve.Length ?? 0.0;

                    const double Eps = 1e-06;
                    if (tailLen > SegmentLengthFeet + Eps)
                    {
                        currentId = tail.Id;
                    }
                    else if (headLen > SegmentLengthFeet + Eps)
                    {
                        currentId = head.Id;
                    }
                    else
                    {
                        // Beide segmenten zijn <= 5000 mm: gereed
                        break;
                    }
                }

                t.Commit();
            }

            tg.Assimilate();
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // User cancel: geen popup nodig
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Fout bij splitten", ex.Message);
        }
    }

    public string GetName() => nameof(Com_SplitPipe);

    private static XYZ PointAlongCurveByLength(Curve curve, double lengthFromStart)
    {
        double total = curve.Length;
        if (lengthFromStart <= 0) return curve.GetEndPoint(0);
        if (lengthFromStart >= total) return curve.GetEndPoint(1);

        double normalized = lengthFromStart / total;
        return curve.Evaluate(normalized, true);
    }

    private static Connector? GetClosestConnector(MEPCurve mep, XYZ point)
    {
        var conns = mep?.ConnectorManager?.Connectors;
        if (conns == null) return null;

        Connector? best = null;
        double bestDist = double.MaxValue;

        foreach (Connector c in conns)
        {
            double d = c.Origin.DistanceTo(point);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }

        return best;
    }

    private class MepCurveSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Pipe || elem is Duct;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}