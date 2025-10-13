namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

public class Com_RecessPlaceBeam : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;
            var doc = uiDoc.Document;

            using (var tx = new Transaction(doc, "Recess: Place in Beam"))
            {
                tx.Start();

                // 1️⃣ Selecteer Pipe
                var sel = uiDoc.Selection;
                Reference mepRef = sel.PickObject(ObjectType.Element, new MepCurveSelectionFilter(), "Selecteer een Pipe");
                if (mepRef == null) throw new OperationCanceledException("Geen MEP-element geselecteerd.");
                var mepElem = doc.GetElement(mepRef);
                if (mepElem is not Pipe pipe)
                    throw new InvalidOperationException("Alleen Pipes worden ondersteund voor funderingssparingen.");

                // 2️⃣ Selecteer host (mag gelinkt zijn)
                Reference hostRef = null;
                try
                {
                    hostRef = sel.PickObject(ObjectType.LinkedElement, "Selecteer het host element (balk/fundering), eventueel in een gelinkt model");
                }
                catch
                {
                    hostRef = sel.PickObject(ObjectType.Element, "Selecteer het host element (balk/fundering)");
                }
                if (hostRef == null) throw new OperationCanceledException("Geen host-element geselecteerd.");

                // 3️⃣ Resolve host element + transform
                Element hostElem;
                Document hostElemDoc = doc;
                Transform toHost = Transform.Identity;

                if (hostRef.LinkedElementId != ElementId.InvalidElementId)
                {
                    var linkInst = doc.GetElement(hostRef.ElementId) as RevitLinkInstance
                                   ?? throw new InvalidOperationException("Kon RevitLinkInstance niet ophalen.");
                    var linkDoc = linkInst.GetLinkDocument()
                                   ?? throw new InvalidOperationException("Gelinkt document is niet geladen.");
                    hostElem = linkDoc.GetElement(hostRef.LinkedElementId)
                               ?? throw new InvalidOperationException("Kon gelinkt element niet ophalen.");
                    hostElemDoc = linkDoc;
                    toHost = linkInst.GetTotalTransform() ?? Transform.Identity;
                }
                else
                {
                    hostElem = doc.GetElement(hostRef.ElementId)
                               ?? throw new InvalidOperationException("Kon host-element niet ophalen.");
                }

                // 4️⃣ Bereken intersectie tussen MEP en host
                var mepSolids = GetElementSolids(doc, mepElem, Transform.Identity);
                var hostSolids = GetElementSolids(hostElemDoc, hostElem, toHost);

                if (mepSolids.Count == 0)
                    throw new InvalidOperationException("Geen solide geometrie gevonden voor de Pipe.");
                if (hostSolids.Count == 0)
                    throw new InvalidOperationException("Geen solide geometrie gevonden voor het host-element.");

                var intersections = new List<Solid>();
                foreach (var ms in mepSolids)
                foreach (var hs in hostSolids)
                {
                    try
                    {
                        var inter = BooleanOperationsUtils.ExecuteBooleanOperation(ms, hs, BooleanOperationsType.Intersect);
                        if (inter != null && inter.Volume > 1e-6)
                            intersections.Add(inter);
                    }
                    catch { }
                }

                if (intersections.Count == 0)
                    throw new InvalidOperationException("Geen doorsnede-volume gevonden tussen Pipe en host.");

                // 5️⃣ Bepaal centrum van doorsnede
                var center = ComputeIntersectionCenter(intersections);
                if (center == null)
                {
                    var interBb = intersections.Select(s => s.GetBoundingBox()).Aggregate((acc, bb) => UnionBb(acc, bb));
                    center = (interBb.Min + interBb.Max) * 0.5;
                }

                // 🔹 Corrigeer Z-hoogte naar hart van de Pipe
                if (mepElem.Location is LocationCurve lc && lc.Curve != null)
                {
                    var pipeLine = lc.Curve as Line ?? Line.CreateBound(lc.Curve.GetEndPoint(0), lc.Curve.GetEndPoint(1));
                    var pipeDir = pipeLine.Direction.Normalize();

                    var start = pipeLine.GetEndPoint(0);
                    var vecToCenter = center - start;
                    var proj = start + pipeDir.Multiply(vecToCenter.DotProduct(pipeDir));

                    center = new XYZ(center.X, center.Y, proj.Z);
                }

                double depth = EstimateDepth(hostElem, intersections, pipe);
                Level level = FindNearestLevel(doc, center) 
                              ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

                if (level == null)
                    throw new InvalidOperationException("Geen Level gevonden voor plaatsing.");

                // 🔧 CORRECTIE: Bereken offset vanaf level
                double offsetFromLevel = center.Z - level.Elevation;
                XYZ placePoint = new XYZ(center.X, center.Y, offsetFromLevel);

                // 6️⃣ Family selecteren en sparing plaatsen
                double openingDiaFeet = MapPipeDiameterToOpening(pipe.Diameter);

                var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing rond", "funderingsparing")
                             ?? throw new InvalidOperationException("Family 'NLRS_00.00_GM_LB_sparing rond' met type 'funderingsparing' niet gevonden.");
                if (!symbol.IsActive) symbol.Activate();
                
                var fi = doc.Create.NewFamilyInstance(placePoint, symbol, level, StructuralType.NonStructural);
                SetParam(fi, "ins_diameter", openingDiaFeet);
                SetParam(fi, "ins_sparing_diepte_totaal", depth);
                SetParam(fi, "ins_instal_status", 0);

                doc.Regenerate();

                // 7️⃣ Uitlijnen met Pipe
                TryAlignRotationToMepInPlan(doc, fi, pipe, angleOffsetDegrees: 0);

                tx.Commit();
            }
        }
        catch (OperationCanceledException)
        {
            // Gebruiker annuleerde selectie
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Recess - Beam", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Recess - Place in Beam";

    // 🔹 Alleen Pipes mogen worden geselecteerd
    private class MepCurveSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is Pipe;
        public bool AllowReference(Reference reference, XYZ position) => true;
    }

    // ----- Helpers -----

    private static IList<Solid> GetElementSolids(Document doc, Element e, Transform toHost)
    {
        var solids = new List<Solid>();
        var opts = new Options
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = true,
            DetailLevel = ViewDetailLevel.Fine
        };
        var ge = e.get_Geometry(opts);
        if (ge == null) return solids;

        void AddSolid(Solid s, Transform t)
        {
            if (s == null || s.Volume <= 1e-6 || s.Faces.IsEmpty) return;
            solids.Add(t == null || t.IsIdentity ? s : SolidUtils.CreateTransformed(s, t));
        }

        void Walk(IEnumerable<GeometryObject> gos, Transform acc)
        {
            foreach (var go in gos)
            {
                switch (go)
                {
                    case Solid s:
                        AddSolid(s, acc);
                        break;
                    case GeometryInstance gi:
                        var t = acc?.Multiply(gi.Transform) ?? gi.Transform;
                        Walk(gi.SymbolGeometry, t);
                        break;
                    case GeometryElement ge2:
                        Walk(ge2, acc);
                        break;
                }
            }
        }

        var baseT = toHost ?? Transform.Identity;
        Walk(ge, baseT);
        return solids;
    }

    private static XYZ ComputeIntersectionCenter(IList<Solid> intersections)
    {
        var pts = new List<XYZ>();
        foreach (var s in intersections)
        {
            try { var c = s.ComputeCentroid(); if (c != null) pts.Add(c); } catch { }
        }
        if (pts.Count == 0) return null;
        double x = pts.Average(p => p.X), y = pts.Average(p => p.Y), z = pts.Average(p => p.Z);
        return new XYZ(x, y, z);
    }

    private static BoundingBoxXYZ UnionBb(BoundingBoxXYZ a, BoundingBoxXYZ b)
    {
        if (a == null) return b;
        if (b == null) return a;
        var min = new XYZ(Math.Min(a.Min.X, b.Min.X), Math.Min(a.Min.Y, b.Min.Y), Math.Min(a.Min.Z, b.Min.Z));
        var max = new XYZ(Math.Max(a.Max.X, b.Max.X), Math.Max(a.Max.Y, b.Max.Y), Math.Max(a.Max.Z, b.Max.Z));
        return new BoundingBoxXYZ { Min = min, Max = max };
    }

    private static double EstimateDepth(Element hostElem, IList<Solid> intersections, Pipe pipe)
    {
        // Diepte = lengte van doorsnede langs de pipe-richting
        if (intersections?.Count > 0)
        {
            var bb = intersections.Select(s => s.GetBoundingBox()).Aggregate((acc, x) => UnionBb(acc, x));
        
            // Bepaal pipe richting
            var pipeDir = GetMepDirection(pipe);
            var pipeDirXY = new XYZ(pipeDir.X, pipeDir.Y, 0).Normalize();
        
            // Bereken afmetingen in X, Y en Z richting
            double lengthX = Math.Abs(bb.Max.X - bb.Min.X);
            double lengthY = Math.Abs(bb.Max.Y - bb.Min.Y);
            double lengthZ = Math.Abs(bb.Max.Z - bb.Min.Z);
        
            // Kies de afmeting die het beste past bij de pipe richting
            double depth;
            if (Math.Abs(pipeDir.Z) > 0.7) // Verticale pipe
                depth = lengthZ;
            else if (Math.Abs(pipeDirXY.X) > 0.7) // Pipe loopt voornamelijk in X-richting
                depth = lengthX;
            else if (Math.Abs(pipeDirXY.Y) > 0.7) // Pipe loopt voornamelijk in Y-richting
                depth = lengthY;
            else // Diagonaal - neem de grootste horizontale afmeting
                depth = Math.Max(lengthX, lengthY);
        
            return Math.Max(depth, UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters));
        }

        var hostBb = hostElem.get_BoundingBox(null);
        if (hostBb != null)
        {
            double lengthX = Math.Abs(hostBb.Max.X - hostBb.Min.X);
            double lengthY = Math.Abs(hostBb.Max.Y - hostBb.Min.Y);
            double lengthZ = Math.Abs(hostBb.Max.Z - hostBb.Min.Z);
            return Math.Max(Math.Max(lengthX, lengthY), lengthZ);
        }

        return UnitUtils.ConvertToInternalUnits(300, UnitTypeId.Millimeters);
    }

    private static Level? FindNearestLevel(Document doc, XYZ p)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .OrderBy(l => Math.Abs(l.Elevation - p.Z))
            .FirstOrDefault();
    }

    private static FamilySymbol? FindFamilySymbol(Document doc, string familyName, string typeName)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(fs =>
                fs.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase) &&
                fs.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetParam(Element e, string name, double value)
    {
        var p = e.LookupParameter(name);
        if (p != null && !p.IsReadOnly) p.Set(value);
    }

    // Richting van de leiding gebruiken om family te roteren
    private static void TryAlignRotationToMepInPlan(Document doc, FamilyInstance fi, Pipe pipe, double angleOffsetDegrees = 0)
    {
        try
        {
            var mepDir3D = GetMepDirection(pipe);
            if (mepDir3D == null || mepDir3D.IsAlmostEqualTo(XYZ.Zero)) return;

            var target = new XYZ(mepDir3D.X, mepDir3D.Y, 0.0).Normalize();
            doc.Regenerate();
            var tf = fi.GetTransform();
            var current = new XYZ(tf.BasisY.X, tf.BasisY.Y, 0.0).Normalize();

            double angle = SignedAngle(current, target, XYZ.BasisZ);
            if (Math.Abs(angleOffsetDegrees) > 1e-9)
                angle += angleOffsetDegrees * Math.PI / 180.0;

            if (Math.Abs(angle) < 1e-9) return;
            var origin = tf.Origin;
            var rotAxis = Line.CreateBound(origin, origin + XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, fi.Id, rotAxis, angle);
        }
        catch { }
    }

    private static double SignedAngle(XYZ v1, XYZ v2, XYZ axis)
    {
        var cross = v1.CrossProduct(v2);
        double sin = cross.GetLength();
        double sign = Math.Sign(cross.DotProduct(axis));
        double cos = v1.DotProduct(v2);
        return Math.Atan2(sign * sin, cos);
    }

    private static XYZ GetMepDirection(Pipe pipe)
    {
        try
        {
            var cm = pipe.ConnectorManager;
            if (cm != null)
            {
                var ends = cm.Connectors.Cast<Connector>()
                    .Where(c => c.ConnectorType == ConnectorType.End)
                    .OrderBy(c => c.Origin.X)
                    .ThenBy(c => c.Origin.Y)
                    .ThenBy(c => c.Origin.Z)
                    .ToList();

                if (ends.Count >= 2)
                {
                    var v = ends[1].Origin - ends[0].Origin;
                    if (!v.IsAlmostEqualTo(XYZ.Zero)) return v.Normalize();
                }
            }
        }
        catch { }

        // Fallback via LocationCurve
        if (pipe.Location is LocationCurve lc && lc.Curve != null)
        {
            if (lc.Curve is Line ln) return ln.Direction.Normalize();
            var der = lc.Curve.ComputeDerivatives(0.5, true);
            var v = der?.BasisX;
            if (v != null && !v.IsAlmostEqualTo(XYZ.Zero)) return v.Normalize();
        }

        return XYZ.BasisX; // fallback
    }

    private static double MapPipeDiameterToOpening(double pipeDiaFeet)
    {
        double pipeDiaMM = UnitUtils.ConvertFromInternalUnits(pipeDiaFeet, UnitTypeId.Millimeters);
        var ranges = new (double min, double max, double openingMM)[]
        {
            (0, 40, 50),
            (41, 50, 75),
            (51, 90, 110),
            (91, 125, 160),
            (126, 160, 200),
        };

        foreach (var (min, max, opening) in ranges)
        {
            if (pipeDiaMM >= min - 1e-6 && pipeDiaMM <= max + 1e-6)
                return UnitUtils.ConvertToInternalUnits(opening, UnitTypeId.Millimeters);
        }

        double roundedUp = Math.Ceiling(pipeDiaMM / 10.0) * 10.0;
        return UnitUtils.ConvertToInternalUnits(roundedUp, UnitTypeId.Millimeters);
    }
}
