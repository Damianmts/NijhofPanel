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

public class Com_RecessPlaceFloor : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            var doc = uiDoc.Document;

            using (var tx = new Transaction(doc, "Recess: Place in Floor"))
            {
                tx.Start();

                // Implementatie voor het plaatsen van een recess in een muur.
                // - Selecteer Pipe of Duct
                // - Selecteer Floor (DirectShape of ander element)
                // - Bepaal start en end van intersection
                // - Plaats Generic Model (Sparing) op hart van intersectie en vul lengte in (NLRS_C_diepte)
                // - Kies juiste grootte (NLRS_C_diameter) op basis van Pipe of Duct diameter
                // - Validaties en foutafhandeling
                
                // - Optioneel uitbreiden voor Cabletray of andere elementen
                
                var sel = uiDoc.Selection;

                // 1) Selecteer MEP (Pipe/Duct)
                Reference mepRef = sel.PickObject(ObjectType.Element, new Com_RecessPlaceFloor.MepCurveSelectionFilter(), "Selecteer een Pipe of Duct");
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
                    // Als gebruiker een native element kiest, fallback:
                    hostRef = sel.PickObject(ObjectType.Element, "Selecteer het host element (muur/vloer/etc.)");
                }
                if (hostRef == null) throw new OperationCanceledException("Geen host-element geselecteerd.");

                // 3) Resolve host element en transform (naar host-doc coördinaten)
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

                // 4) Haal solids op (view-onafhankelijk) en bereken intersecties
                var mepSolids = GetElementSolids(doc, mepElem, Transform.Identity);
                var hostSolids = GetElementSolids(hostElemDoc, hostElem, toHost); // naar host-ruimte getransformeerd

                if (mepSolids.Count == 0)
                    throw new InvalidOperationException("Geen solide geometrie gevonden voor MEP-element.");
                if (hostSolids.Count == 0)
                    throw new InvalidOperationException("Geen solide geometrie gevonden voor host-element.");

                // Snelle bbox precheck
                var mepBb = mepElem.get_BoundingBox(null);
                var hostBb = TransformBoundingBox(hostElem.get_BoundingBox(null), toHost);
                if (mepBb == null || hostBb == null || !BboxOverlap(mepBb, hostBb))
                    throw new InvalidOperationException("Geen overlap gedetecteerd tussen MEP en host (bbox).");

                var intersections = new List<Solid>();
                foreach (var ms in mepSolids)
                foreach (var hs in hostSolids)
                {
                    Solid inter = null;
                    try
                    {
                        inter = BooleanOperationsUtils.ExecuteBooleanOperation(ms, hs, BooleanOperationsType.Intersect);
                    }
                    catch
                    {
                        inter = null;
                    }
                    if (inter != null && inter.Volume > 1e-6) intersections.Add(inter);
                }

                if (intersections.Count == 0)
                    throw new InvalidOperationException("Geen doorsnede-volume gevonden tussen MEP en host.");

                // 5) Bepaal plaatsingspunt en benodigde maten
                var center = ComputeIntersectionCenter(intersections);
                if (center == null)
                {
                    // Fallback op bbox-midden als centroid niet lukt
                    var interBb = intersections
                        .Select(s => s.GetBoundingBox()) // indien API-versie dit niet ondersteunt, vervang door fallback
                        .Aggregate((acc, bb) => UnionBb(acc, bb));
                    center = (interBb.Min + interBb.Max) * 0.5;
                }
                
                bool isPipe = mepElem is Pipe;
                bool isDuct = mepElem is Duct;

                if (!isPipe && !isDuct)
                    throw new InvalidOperationException("Alleen Pipes of Ducts worden ondersteund in deze tool.");

                double depth = EstimateDepth(hostElem, intersections);
                Level level = FindNearestLevel(doc, center) ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

                if (level == null)
                    throw new InvalidOperationException("Geen Level gevonden voor plaatsing.");

                // 6) Kies family en plaats instance
                if (isPipe)
                {
                    var pipe = (Pipe)mepElem;
                    // Kies sparingsdiameter o.b.v. diameter-ranges (mm) -> interne units (ft)
                    double openingDiaFeet = MapPipeDiameterToOpening(pipe.Diameter);

                    // Gebruik specifieke family + type
                    var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing rond", "vloersparing")
                                 ?? throw new InvalidOperationException("Family 'NLRS_00.00_GM_LB_sparing rond' met type 'vloersparing' niet gevonden.");
                    if (!symbol.IsActive) symbol.Activate();

                    var fi = doc.Create.NewFamilyInstance(center, symbol, level, StructuralType.NonStructural);
                    SetParam(fi, "ins_diameter", openingDiaFeet);
                    SetParam(fi, "ins_sparing_diepte_totaal", depth);

                    // Forceer geldige transform na plaatsing/parameters
                    doc.Regenerate();

                    // Plan-rotatie: uitlijnen met pipe in XY, rotatie rond Z
                    TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                }
                else if (isDuct)
                {
                    var duct = (Duct)mepElem;
                    var ductType = doc.GetElement(duct.GetTypeId()) as DuctType;
                    var shape = ductType?.Shape;

                    // NB: Als de marge in de sparing-family zit, hier géén clearance toevoegen.
                    // Round ⇒ ronde sparing op basis van duct diameter (zelfde mapping als pipe)
                    // Rectangular/Square ⇒ rechthoekige sparing, breedte/hoogte = duct.Width/duct.Height
                    // Oval ⇒ ovale sparing, major/minor = duct.Width/duct.Height

                    if (shape == ConnectorProfileType.Round)
                    {
                        double openingDiaFeet = MapPipeDiameterToOpening(duct.Diameter);

                        // Pas familienaam/typename aan indien nodig
                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing rond", "vloersparing")
                                     ?? throw new InvalidOperationException("Family 'NLRS_00.00_GM_LB_sparing rond' met type 'vloersparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(center, symbol, level, StructuralType.NonStructural);
                        // Pas parameternamen aan indien jouw family andere namen gebruikt
                        SetParam(fi, "ins_diameter", openingDiaFeet);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);

                        doc.Regenerate();
                        TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                    }
                    else if (shape == ConnectorProfileType.Rectangular)
                    {
                        double width = duct.Width;   // géén extra marge in code
                        double height = duct.Height; // géén extra marge in code

                        // Rechthoekige sparing family
                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing recht", "vloersparing")
                                     ?? throw new InvalidOperationException("Familie 'NLRS_00.00_GM_LB_sparing recht' met type 'vloersparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(center, symbol, level, StructuralType.NonStructural);
                        // Pas parameternamen aan naar de parameters die jouw sparing-family verwacht
                        SetParam(fi, "ins_breedte", width);
                        SetParam(fi, "ins_hoogte", height);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);

                        doc.Regenerate();
                        TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                    }
                    else if (shape == ConnectorProfileType.Oval)
                    {
                        double width = duct.Width;   // géén extra marge in code
                        double height = duct.Height; // géén extra marge in code

                        // Rechthoekige sparing family
                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing recht", "vloersparing")
                                     ?? throw new InvalidOperationException("Familie 'NLRS_00.00_GM_LB_sparing recht' met type 'vloersparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(center, symbol, level, StructuralType.NonStructural);
                        // Pas parameternamen aan naar de parameters die jouw sparing-family verwacht
                        SetParam(fi, "ins_breedte", width);
                        SetParam(fi, "ins_hoogte", height);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);

                        doc.Regenerate();
                        TryAlignRotationToMepInPlan(doc, fi, mepCurve, angleOffsetDegrees: 0);
                    }
                    else
                    {
                        // Fallback: behandel als rechthoekig zonder extra marge
                        double width = duct.Width;
                        double height = duct.Height;

                        // Rechthoekige sparing family
                        var symbol = FindFamilySymbol(doc, "NLRS_00.00_GM_LB_sparing recht", "vloersparing")
                                     ?? throw new InvalidOperationException("Familie 'NLRS_00.00_GM_LB_sparing recht' met type 'vloersparing' niet gevonden.");
                        if (!symbol.IsActive) symbol.Activate();

                        var fi = doc.Create.NewFamilyInstance(center, symbol, level, StructuralType.NonStructural);
                        // Pas parameternamen aan naar de parameters die jouw sparing-family verwacht
                        SetParam(fi, "ins_breedte", width);
                        SetParam(fi, "ins_hoogte", height);
                        SetParam(fi, "ins_sparing_diepte_totaal", depth);

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
            TaskDialog.Show("Recess - Floor", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Recess - Place in Floor";

    // Helpers

    private class MepCurveSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is MEPCurve; // Pipe/Duct/CableTray/etc.
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
            catch
            {
                // Sommige solids kunnen geen centroid teruggeven; negeer en val later terug
            }
        }

        if (pts.Count == 0) return null;

        // Gemiddelde van alle centroids
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

    private static double EstimateDepth(Element hostElem, IList<Solid> intersections)
    {
        // Voorkeurslogica: als host een Wall is, gebruik wanddikte
        if (hostElem is Wall w) return w.Width;

        // Anders: gebruik Z-range van intersectie als benadering
        var bb = intersections.Select(s => s.GetBoundingBox()).Aggregate((acc, x) => UnionBb(acc, x));
        return Math.Max(0.001, bb.Max.Z - bb.Min.Z); // minimale diepte fallback
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

    // Plan-view uitlijning: roteer rond Z zodat family-forward naar MEP-projectie wijst
    private static void TryAlignRotationToMepInPlan(Document doc, FamilyInstance fi, MEPCurve mep, double angleOffsetDegrees = 0)
    {
        try
        {
            // 1) Bepaal MEP-richting en projecteer op XY
            var mepDir3D = GetMepDirection(mep);
            if (mepDir3D == null || mepDir3D.IsAlmostEqualTo(XYZ.Zero)) return;
            var target = new XYZ(mepDir3D.X, mepDir3D.Y, 0.0);
            if (target.IsAlmostEqualTo(XYZ.Zero)) return;
            target = target.Normalize();

            // 2) Bepaal huidige “forward” van de family en projecteer op XY
            doc.Regenerate();
            var tf = fi.GetTransform();

            // Voorkeur: BasisY als “verticale” symboolrichting in plan. Pas aan naar BasisX als jouw family dat gebruikt.
            XYZ current = tf.BasisY;
            if (current == null || current.IsAlmostEqualTo(XYZ.Zero)) current = tf.BasisX;
            if (current == null || current.IsAlmostEqualTo(XYZ.Zero)) return;
            current = new XYZ(current.X, current.Y, 0.0);
            if (current.IsAlmostEqualTo(XYZ.Zero)) return;
            current = current.Normalize();

            // 3) Bepaal signed delta-hoek in XY en roteer rond Z-as door het instance-centrum
            double angle = SignedAngle(current, target, XYZ.BasisZ);

            // Eventuele vaste offset (bv. 90° als family langs X is getekend)
            if (Math.Abs(angleOffsetDegrees) > 1e-9)
                angle += angleOffsetDegrees * Math.PI / 180.0;

            if (Math.Abs(angle) < 1e-9) return;

            var origin = tf.Origin;
            var rotAxis = Line.CreateBound(origin, origin + XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, fi.Id, rotAxis, angle);
        }
        catch
        {
            // optioneel: logging
        }
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
            // Probeer via end connectors
            var cm = mep.ConnectorManager;
            if (cm != null)
            {
                var ends = cm.Connectors.Cast<Connector>()
                    .Where(c => c.ConnectorType == ConnectorType.End)
                    .OrderBy(c => c.Origin.X) // stabiele volgorde, willekeurige sleutel
                    .ThenBy(c => c.Origin.Y)
                    .ThenBy(c => c.Origin.Z)
                    .ToList();

                if (ends.Count >= 2)
                {
                    var v = (ends[1].Origin - ends[0].Origin);
                    if (!v.IsAlmostEqualTo(XYZ.Zero)) return v.Normalize();
                }
            }

            // Fallback: LocationCurve
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
        catch
        {
            // negeer; we geven null terug bij falen
        }
        return null!;
    }
    
    // Pipe diameter (ft) -> opening diameter (ft) via ranges gedefinieerd in mm
    private static double MapPipeDiameterToOpening(double pipeDiaFeet)
    {
        double pipeDiaMM = UnitUtils.ConvertFromInternalUnits(pipeDiaFeet, UnitTypeId.Millimeters);

        // Definieer ranges in mm (inclusief bovengrens)
        // Voorbeeld: 50–75 mm → 75 mm
        var ranges = new (double min, double max, double openingMM)[]
        {
            (0,   40,  50),
            (41,  50,  75),
            (51,  90, 110),
            (91, 125, 160),
            (126, 160, 200),
            // Voeg zo nodig meer ranges toe...
        };

        foreach (var (min, max, opening) in ranges)
        {
            bool inRange = (pipeDiaMM >= min - 1e-6) && (pipeDiaMM <= max + 1e-6);
            if (inRange)
                return UnitUtils.ConvertToInternalUnits(opening, UnitTypeId.Millimeters);
        }

        // Fallback: rond naar boven op 10 mm
        double roundedUp = Math.Ceiling(pipeDiaMM / 10.0) * 10.0;
        return UnitUtils.ConvertToInternalUnits(roundedUp, UnitTypeId.Millimeters);
    }
}