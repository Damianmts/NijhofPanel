namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

public class Com_RecessPlaceWall : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            var doc = uiDoc.Document;

            using (var tx = new Transaction(doc, "Recess: Place in Wall"))
            {
                tx.Start();

                var sel = uiDoc.Selection;

                // 1) Selecteer MEP (Pipe/Duct)
                Reference mepRef = sel.PickObject(ObjectType.Element, new MepCurveSelectionFilter(), "Selecteer een Pipe of Duct");
                if (mepRef == null) throw new OperationCanceledException("Geen MEP-element geselecteerd.");
                var mepElem = doc.GetElement(mepRef);
                if (mepElem is not MEPCurve mepCurve)
                    throw new InvalidOperationException("Geselecteerd element is geen MEPCurve (Pipe/Duct).");

                // 2) Selecteer host (mag gelinkt zijn)
                Reference hostRef = null;
                try
                {
                    hostRef = sel.PickObject(ObjectType.LinkedElement, "Selecteer het host element (muur/vloer/etc.), eventueel in een gelinkt model");
                }
                catch
                {
                    hostRef = sel.PickObject(ObjectType.Element, "Selecteer het host element (muur/vloer/etc.)");
                }
                if (hostRef == null) throw new OperationCanceledException("Geen host-element geselecteerd.");

                // 3) Resolve host element en transform
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

                // 4) Haal solids op en bereken intersecties
                var mepSolids = GetElementSolids(doc, mepElem, Transform.Identity);
                var hostSolids = GetElementSolids(hostElemDoc, hostElem, toHost);

                if (mepSolids.Count == 0)
                    throw new InvalidOperationException("Geen solide geometrie gevonden voor MEP-element.");
                if (hostSolids.Count == 0)
                    throw new InvalidOperationException("Geen solide geometrie gevonden voor host-element.");

                var mepBb = mepElem.get_BoundingBox(null);
                var hostBb = TransformBoundingBox(hostElem.get_BoundingBox(null), toHost);
                if (mepBb == null || hostBb == null || !BboxOverlap(mepBb, hostBb))
                    throw new InvalidOperationException("Geen overlap gedetecteerd tussen MEP en host (bbox).");

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
                    throw new InvalidOperationException("Geen doorsnede-volume gevonden tussen MEP en host.");

                // 5) Bepaal plaatsingspunt en benodigde maten
                var center = ComputeIntersectionCenter(intersections);
                if (center == null)
                {
                    var interBb = intersections.Select(s => s.GetBoundingBox()).Aggregate((acc, bb) => UnionBb(acc, bb));
                    center = (interBb.Min + interBb.Max) * 0.5;
                }

                bool isPipe = mepElem is Pipe;
                bool isDuct = mepElem is Duct;

                if (!isPipe && !isDuct)
                    throw new InvalidOperationException("Alleen Pipes of Ducts worden ondersteund in deze tool.");

                // 🔹 Corrigeer Z-hoogte naar hart van de MEP
                if (mepElem.Location is LocationCurve lc && lc.Curve != null)
                {
                    var mepLine = lc.Curve as Line ?? Line.CreateBound(lc.Curve.GetEndPoint(0), lc.Curve.GetEndPoint(1));
                    var mepDir = mepLine.Direction.Normalize();

                    var start = mepLine.GetEndPoint(0);
                    var vecToCenter = center - start;
                    var proj = start + mepDir.Multiply(vecToCenter.DotProduct(mepDir));

                    center = new XYZ(center.X, center.Y, proj.Z);
                }

                double depth = EstimateDepth(hostElem, intersections, mepCurve);
                Level level = FindNearestLevel(doc, center) 
                              ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

                if (level == null)
                    throw new InvalidOperationException("Geen Level gevonden voor plaatsing.");

                // 🔧 Bereken offset vanaf level
                double offsetFromLevel = center.Z - level.Elevation;
                XYZ placePoint = new XYZ(center.X, center.Y, offsetFromLevel);

                // 6) Kies family en plaats instance
                if (isPipe)
                {
                    var pipe = (Pipe)mepElem;
                    double openingDiaFeet = MapPipeDiameterToOpening(pipe.Diameter);

                    var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing rond", "wandsparing")
                                 ?? throw new InvalidOperationException("Family 'NLRS_00.00_GM_LB_sparing rond' met type 'wandsparing' niet gevonden.");
                    if (!symbol.IsActive) symbol.Activate();

                    var fi = doc.Create.NewFamilyInstance(placePoint, symbol, level, StructuralType.NonStructural);
                    SetParam(fi, "ins_diameter", openingDiaFeet);
                    SetParam(fi, "ins_sparing_diepte_totaal", depth);
                    SetParam(fi, "ins_instal_status", 0);

                    doc.Regenerate();
                    TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                }
                else if (isDuct)
                {
                    var duct = (Duct)mepElem;
                    var ductType = doc.GetElement(duct.GetTypeId()) as DuctType;
                    var shape = ductType?.Shape;

                    if (shape == ConnectorProfileType.Round)
                    {
                        double openingDiaFeet = MapPipeDiameterToOpening(duct.Diameter);

                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing rond", "wandsparing")
                                     ?? throw new InvalidOperationException("Family 'NLRS_00.00_GM_LB_sparing rond' met type 'wandsparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(placePoint, symbol, level, StructuralType.NonStructural);
                        SetParam(fi, "ins_diameter", openingDiaFeet);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);
                        SetParam(fi, "ins_instal_status", 0);

                        doc.Regenerate();
                        TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                    }
                    else if (shape == ConnectorProfileType.Rectangular || shape == ConnectorProfileType.Oval)
                    {
                        double width = duct.Width;
                        double height = duct.Height;

                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing recht", "wandsparing")
                                     ?? throw new InvalidOperationException("Familie 'NLRS_00.00_GM_LB_sparing recht' met type 'wandsparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(placePoint, symbol, level, StructuralType.NonStructural);
                        SetParam(fi, "ins_breedte", width);
                        SetParam(fi, "ins_hoogte", height);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);
                        SetParam(fi, "ins_instal_status", 0);

                        doc.Regenerate();
                        TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                    }
                    else
                    {
                        // Fallback: behandel als rechthoekig
                        double width = duct.Width;
                        double height = duct.Height;

                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing recht", "wandsparing")
                                     ?? throw new InvalidOperationException("Familie 'NLRS_00.00_GM_LB_sparing recht' met type 'wandsparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(placePoint, symbol, level, StructuralType.NonStructural);
                        SetParam(fi, "ins_breedte", width);
                        SetParam(fi, "ins_hoogte", height);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);
                        SetParam(fi, "ins_instal_status", 0);

                        doc.Regenerate();
                        TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                    }
                }
                tx.Commit();
            }
        }
        catch (OperationCanceledException)
        {
            // User cancel: niets tonen
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Recess - Wall", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Recess - Place in Wall";

    // Helpers

    private class MepCurveSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is MEPCurve;
        public bool AllowReference(Reference reference, XYZ position) => true;
    }

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
            try
            {
                var c = s.ComputeCentroid();
                if (c != null) pts.Add(c);
            }
            catch { }
        }

        if (pts.Count == 0) return null;

        double x = 0, y = 0, z = 0;
        foreach (var p in pts)
        {
            x += p.X; y += p.Y; z += p.Z;
        }
        return new XYZ(x / pts.Count, y / pts.Count, z / pts.Count);
    }

    private static BoundingBoxXYZ TransformBoundingBox(BoundingBoxXYZ bb, Transform t)
    {
        if (bb == null) return null;
        if (t == null || t.IsIdentity) return bb;

        var pts = new[]
        {
            new XYZ(bb.Min.X, bb.Min.Y, bb.Min.Z),
            new XYZ(bb.Min.X, bb.Min.Y, bb.Max.Z),
            new XYZ(bb.Min.X, bb.Max.Y, bb.Min.Z),
            new XYZ(bb.Min.X, bb.Max.Y, bb.Max.Z),
            new XYZ(bb.Max.X, bb.Min.Y, bb.Min.Z),
            new XYZ(bb.Max.X, bb.Min.Y, bb.Max.Z),
            new XYZ(bb.Max.X, bb.Max.Y, bb.Min.Z),
            new XYZ(bb.Max.X, bb.Max.Y, bb.Max.Z),
        }.Select(p => t.OfPoint(p));

        var min = new XYZ(pts.Min(p => p.X), pts.Min(p => p.Y), pts.Min(p => p.Z));
        var max = new XYZ(pts.Max(p => p.X), pts.Max(p => p.Y), pts.Max(p => p.Z));
        return new BoundingBoxXYZ { Min = min, Max = max };
    }

    private static bool BboxOverlap(BoundingBoxXYZ a, BoundingBoxXYZ b)
    {
        if (a == null || b == null) return false;
        return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X ||
                 a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y ||
                 a.Max.Z < b.Min.Z || a.Min.Z > b.Max.Z);
    }

    private static BoundingBoxXYZ UnionBb(BoundingBoxXYZ a, BoundingBoxXYZ b)
    {
        if (a == null) return b;
        if (b == null) return a;
        var min = new XYZ(Math.Min(a.Min.X, b.Min.X), Math.Min(a.Min.Y, b.Min.Y), Math.Min(a.Min.Z, b.Min.Z));
        var max = new XYZ(Math.Max(a.Max.X, b.Max.X), Math.Max(a.Max.Y, b.Max.Y), Math.Max(a.Max.Z, b.Max.Z));
        return new BoundingBoxXYZ { Min = min, Max = max };
    }

    private static double EstimateDepth(Element hostElem, IList<Solid> intersections, MEPCurve mep)
    {
        // Voor muren: gebruik wanddikte als primaire bron
        if (hostElem is Wall w) return w.Width;

        // Voor andere elementen: bereken lengte langs MEP-richting
        if (intersections?.Count > 0)
        {
            var bb = intersections.Select(s => s.GetBoundingBox()).Aggregate((acc, x) => UnionBb(acc, x));

            double lengthX = Math.Abs(bb.Max.X - bb.Min.X);
            double lengthY = Math.Abs(bb.Max.Y - bb.Min.Y);
            double lengthZ = Math.Abs(bb.Max.Z - bb.Min.Z);

            var mepDir = GetMepDirection(mep);
            if (mepDir != null && !mepDir.IsAlmostEqualTo(XYZ.Zero))
            {
                var mepDirXY = new XYZ(mepDir.X, mepDir.Y, 0).Normalize();

                // Kies afmeting op basis van MEP-richting
                if (Math.Abs(mepDir.Z) > 0.7) // Verticaal
                    return Math.Max(lengthZ, UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters));
                else if (Math.Abs(mepDirXY.X) > 0.7) // X-richting
                    return Math.Max(lengthX, UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters));
                else if (Math.Abs(mepDirXY.Y) > 0.7) // Y-richting
                    return Math.Max(lengthY, UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters));
                else // Diagonaal
                    return Math.Max(Math.Max(lengthX, lengthY), UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters));
            }

            // Fallback: grootste afmeting
            double depth = Math.Max(Math.Max(lengthX, lengthY), lengthZ);
            return Math.Max(depth, UnitUtils.ConvertToInternalUnits(50, UnitTypeId.Millimeters));
        }

        // Laatste fallback
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

    private static FamilySymbol? FindFamilySymbol(Document doc, string familyName)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(fs => fs.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase));
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

    private static void TryAlignRotationToMepInPlan(Document doc, FamilyInstance fi, MEPCurve mep, double angleOffsetDegrees = 0)
    {
        try
        {
            var mepDir3D = GetMepDirection(mep);
            if (mepDir3D == null || mepDir3D.IsAlmostEqualTo(XYZ.Zero)) return;
            var target = new XYZ(mepDir3D.X, mepDir3D.Y, 0.0);
            if (target.IsAlmostEqualTo(XYZ.Zero)) return;
            target = target.Normalize();

            doc.Regenerate();
            var tf = fi.GetTransform();

            XYZ current = tf.BasisY;
            if (current == null || current.IsAlmostEqualTo(XYZ.Zero)) current = tf.BasisX;
            if (current == null || current.IsAlmostEqualTo(XYZ.Zero)) return;
            current = new XYZ(current.X, current.Y, 0.0);
            if (current.IsAlmostEqualTo(XYZ.Zero)) return;
            current = current.Normalize();

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

    private static XYZ GetMepDirection(MEPCurve mep)
    {
        try
        {
            var cm = mep.ConnectorManager;
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
                    var v = (ends[1].Origin - ends[0].Origin);
                    if (!v.IsAlmostEqualTo(XYZ.Zero)) return v.Normalize();
                }
            }

            if (mep.Location is LocationCurve lc && lc.Curve != null)
            {
                if (lc.Curve is Line ln)
                    return ln.Direction.Normalize();

                var der = lc.Curve.ComputeDerivatives(0.5, true);
                var v = der?.BasisX;
                if (v != null && !v.IsAlmostEqualTo(XYZ.Zero)) return v.Normalize();

                var v2 = lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0);
                if (!v2.IsAlmostEqualTo(XYZ.Zero)) return v2.Normalize();
            }
        }
        catch { }
        return null!;
    }

    private static double MapPipeDiameterToOpening(double pipeDiaFeet)
    {
        double pipeDiaMM = UnitUtils.ConvertFromInternalUnits(pipeDiaFeet, UnitTypeId.Millimeters);

        var ranges = new (double min, double max, double openingMM)[]
        {
            (0,   40,  50),
            (41,  50,  75),
            (51,  90, 110),
            (91, 125, 160),
            (126, 160, 200),
        };

        foreach (var (min, max, opening) in ranges)
        {
            bool inRange = (pipeDiaMM >= min - 1e-6) && (pipeDiaMM <= max + 1e-6);
            if (inRange)
                return UnitUtils.ConvertToInternalUnits(opening, UnitTypeId.Millimeters);
        }

        double roundedUp = Math.Ceiling(pipeDiaMM / 10.0) * 10.0;
        return UnitUtils.ConvertToInternalUnits(roundedUp, UnitTypeId.Millimeters);
    }
}