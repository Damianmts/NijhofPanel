namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

public class Com_Create3DView : IExternalEventHandler
{
    public void Execute(UIApplication uiApp)
    {
        UIDocument uidoc = uiApp.ActiveUIDocument;
        Document doc = uidoc.Document;

        try
        {
            // Controleer of actieve view een sheet is
            View activeView = doc.ActiveView;
            if (activeView is not ViewSheet sheet)
            {
                TaskDialog.Show("Fout", "Deze functie werkt alleen vanuit een sheet (buiten een viewport).");
                return;
            }

            // Gebruiker selecteert een viewport
            Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Selecteer een viewport op de sheet.");
            Viewport viewport = (doc.GetElement(pickedRef) as Viewport)!;
            if (viewport == null)
            {
                TaskDialog.Show("Fout", "Geen geldige viewport geselecteerd.");
                return;
            }

            View planView = (doc.GetElement(viewport.ViewId) as View)!;
            if (planView == null || planView.ViewType is ViewType.Legend or ViewType.ThreeD)
            {
                TaskDialog.Show("Fout", "Selecteer een plattegrond (geen legenda of 3D).");
                return;
            }

            // Verzamel elementen in de geselecteerde view
            var filter = new ElementMulticategoryFilter(new List<BuiltInCategory>
            {
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_DuctFitting,
            });

            var collector = new FilteredElementCollector(doc, planView.Id)
                .WherePasses(filter)
                .WhereElementIsNotElementType()
                .ToList();

            if (collector.Count == 0)
            {
                TaskDialog.Show("Fout", "Geen leidingen of kanalen gevonden in de geselecteerde view.");
                return;
            }

            var elementsToHide = new List<ElementId>();

            foreach (var elem in collector)
            {
                if (IsVertical(elem))
                    elementsToHide.Add(elem.Id);
            }
            
            // Bereken gecombineerde boundingbox
            BoundingBoxXYZ combinedBox = GetCombinedBoundingBox(collector);
            if (combinedBox == null)
            {
                TaskDialog.Show("Fout", "Kon geen geldige bounding box berekenen.");
                return;
            }
            
            // Laat gebruiker eerst een 3D-template kiezen via het WPF-venster
            var templateView = NijhofPanel.Helpers.Core.ComboBoxHelper.SelectViewTemplate(doc);
            if (templateView == null)
            {
                TaskDialog.Show("Geannuleerd", "Geen template geselecteerd. De actie is afgebroken.");
                return;
            }
            
            using (Transaction t = new Transaction(doc, "Maak 3D prefab view"))
            {
                t.Start();

                // Maak nieuwe 3D view
                ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.ThreeDimensional);

                if (viewFamilyType == null)
                {
                    TaskDialog.Show("Fout", "Geen 3D view family type gevonden.");
                    return;
                }

                View3D view3D = View3D.CreateIsometric(doc, viewFamilyType.Id);

                if (elementsToHide.Count > 0)
                    view3D.HideElements(elementsToHide);
                
                // Unieke naam
                string sheetNumber = sheet.get_Parameter(BuiltInParameter.SHEET_NUMBER).AsString();
                string baseName = $"Nr {sheetNumber} - 3D view";
                view3D.Name = GetUniqueViewName(doc, baseName);

                // Section box instellen
                view3D.ViewTemplateId = templateView.Id; // gebruik geselecteerde template
                view3D.IsSectionBoxActive = true;
                view3D.SetSectionBox(combinedBox);

                // Oriëntatie instellen
                SetViewOrientation(view3D, combinedBox);

                // Plaats nieuwe 3D view op sheet
                Viewport.Create(doc, sheet.Id, view3D.Id, viewport.GetBoxCenter());

                t.Commit();
            }

            TaskDialog.Show("Succes", "3D view is aangemaakt en geplaatst op de sheet.");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // Gebruiker annuleerde selectie → geen foutmelding
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Fout", ex.Message);
        }
    }

    public string GetName() => nameof(Com_Create3DView);

    // Zoek naar verticale ducts of pipes
    private static bool IsVertical(Element e)
    {
        if (e.Location is not LocationCurve locCurve) 
            return false;

        XYZ p1 = locCurve.Curve.GetEndPoint(0);
        XYZ p2 = locCurve.Curve.GetEndPoint(1);
        XYZ dir = (p2 - p1).Normalize();

        // Als de Z-component bijna 1 of -1 is → verticale richting
        return Math.Abs(dir.Z) > 0.8;
    }
    
    // Bereken gecombineerde boundingbox van elementen
    private static BoundingBoxXYZ GetCombinedBoundingBox(IEnumerable<Element> elements)
    {
        BoundingBoxXYZ totalBox = null!;
        foreach (Element e in elements)
        {
            BoundingBoxXYZ box = e.get_BoundingBox(null);
            if (box == null) continue;

            if (totalBox == null)
            {
                totalBox = new BoundingBoxXYZ
                {
                    Min = box.Min,
                    Max = box.Max
                };
            }
            else
            {
                totalBox.Min = new XYZ(
                    Math.Min(totalBox.Min.X, box.Min.X),
                    Math.Min(totalBox.Min.Y, box.Min.Y),
                    Math.Min(totalBox.Min.Z, box.Min.Z));

                totalBox.Max = new XYZ(
                    Math.Max(totalBox.Max.X, box.Max.X),
                    Math.Max(totalBox.Max.Y, box.Max.Y),
                    Math.Max(totalBox.Max.Z, box.Max.Z));
            }
        }

        if (totalBox != null)
        {
            // Voeg marge toe (0.4 m)
            totalBox.Min -= new XYZ(0.4, 0.4, 0.4);
            totalBox.Max += new XYZ(0.4, 0.4, 0.4);
        }

        return totalBox!;
    }

    // Unieke naam genereren
    private static string GetUniqueViewName(Document doc, string baseName)
    {
        var existingNames = new FilteredElementCollector(doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(v => v.ViewType == ViewType.ThreeD)
            .Select(v => v.Name)
            .ToList();

        string name = baseName;
        int counter = 1;
        while (existingNames.Contains(name))
        {
            name = $"{baseName} {counter++}";
        }

        return name;
    }

    // Stel camera/oriëntatie in (top-front-right)
    private static void SetViewOrientation(View3D view3D, BoundingBoxXYZ box)
    {
        XYZ center = (box.Min + box.Max) / 2;

        XYZ eyeDir = new XYZ(1, 1, 1).Normalize();
        XYZ eyePosition = center + eyeDir.Multiply(5);

        XYZ forward = (center - eyePosition).Normalize();

        XYZ tempUp = new XYZ(0, 0, 1);
        if (Math.Abs(forward.DotProduct(tempUp)) > 0.99)
            tempUp = new XYZ(0, 1, 0);

        XYZ right = forward.CrossProduct(tempUp).Normalize();
        XYZ up = right.CrossProduct(forward).Normalize();

        ViewOrientation3D orientation = new ViewOrientation3D(eyePosition, up, forward);
        view3D.SetOrientation(orientation);
        view3D.SaveOrientationAndLock();
    }
}