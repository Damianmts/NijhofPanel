namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class Com_PrefabCreateSheetsAndViews : IExternalEventHandler
{
    private readonly string _prefabSetNumber;

    public Com_PrefabCreateSheetsAndViews(string prefabSetNumber)
    {
        _prefabSetNumber = prefabSetNumber;
    }

    // Lege constructor voor compatibiliteit
    public Com_PrefabCreateSheetsAndViews() { }

    public void Execute(UIApplication app)
    {
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;

        using (Transaction trans = new Transaction(doc, "Prefab Sheets & Views"))
        {
            trans.Start();

            try
            {
                // Zoek prefab-elementen met de juiste set
                var prefabElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .Where(e => e.LookupParameter("Prefab Set")?.AsString() == _prefabSetNumber)
                    .ToList();

                if (!prefabElements.Any())
                {
                    TaskDialog.Show("Prefab", $"Geen elementen gevonden voor Prefab Set {_prefabSetNumber}.");
                    return;
                }

                // Basisinfo uit eerste element
                Element first = prefabElements.First();
                string verdieping = first.LookupParameter("Prefab Verdieping")?.AsString() ?? "Onbekend";
                string kavel = first.LookupParameter("Prefab Kavelnummer")?.AsString() ?? "Onbekend";
                string systeem = first.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM)?.AsString();

                // Fallback als het via BuiltInParameter niet lukt
                if (string.IsNullOrWhiteSpace(systeem))
                {
                    systeem = first.LookupParameter("System Abbreviation")?.AsString();
                }

                // Normaliseer de waarde voor vergelijking
                systeem = systeem?.Trim().ToUpperInvariant() ?? "ONBEKEND";


                // Views per systeem
                var viewTemplates = new Dictionary<string, List<string>>
                {
                    { "M524", new List<string> { "P52_Riolering", "P50_Lucht_Riolering_Maatvoering", "3D_P52_Riolering" } },
                    { "M521", new List<string> { "P52_Riolering", "3D_P52_Riolering" } },
                    { "M570", new List<string> { "P57_Lucht", "P50_Lucht_Riolering_Maatvoering", "3D_P57_Lucht" } },
                };

                if (!viewTemplates.ContainsKey(systeem))
                {
                    TaskDialog.Show("Prefab", $"Geen view-configuratie voor systeem {systeem}.");
                    return;
                }

                // Zoek alle bestaande views
                var allViews = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .ToList();

                List<View> createdViews = new();

                foreach (string templateName in viewTemplates[systeem])
                {
                    View? baseView = null;

                    // Voor planviews: gebruik verdieping (V00 - ...)
                    if (!templateName.StartsWith("3D", StringComparison.OrdinalIgnoreCase))
                    {
                        string expectedName = $"{verdieping} - {templateName}";
                        baseView = allViews.FirstOrDefault(v => v.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        // Voor 3D-views
                        string expectedName = $"3D - {templateName.Replace("3D_", "")}";
                        baseView = allViews.FirstOrDefault(v => v.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (baseView == null)
                    {
                        TaskDialog.Show("Prefab", $"View '{templateName}' niet gevonden voor {verdieping}.");
                        continue;
                    }

                    View newView = null;

                    // Planviews → dependent view
                    if (baseView is ViewPlan plan)
                    {
                        ElementId newViewId = plan.Duplicate(ViewDuplicateOption.AsDependent);
                        newView = doc.GetElement(newViewId) as View;
                    }
                    // 3D-views → duplicaat
                    else if (baseView.ViewType == ViewType.ThreeD)
                    {
                        ElementId newViewId = baseView.Duplicate(ViewDuplicateOption.WithDetailing);
                        newView = doc.GetElement(newViewId) as View;
                    }

                    if (newView == null)
                        continue;

                    if (newView.ViewType == ViewType.ThreeD)
                    {
                        // Zorg dat naam consistent is met conventie "3D - ..."
                        string viewBase = templateName.Replace("3D_", "");
                        newView.Name = $"3D - {viewBase} {kavel} ({verdieping})";
                    }
                    else
                    {
                        newView.Name = $"{templateName} {kavel} ({verdieping})";
                    }
                    
                    // Crop / Section box rondom prefab elementen
                    BoundingBoxXYZ combinedBox = null;

                    foreach (var el in prefabElements)
                    {
                        BoundingBoxXYZ bb = el.get_BoundingBox(newView);
                        if (bb == null) continue;

                        if (combinedBox == null)
                        {
                            combinedBox = new BoundingBoxXYZ
                            {
                                Min = bb.Min,
                                Max = bb.Max
                            };
                        }
                        else
                        {
                            combinedBox.Min = new XYZ(
                                Math.Min(combinedBox.Min.X, bb.Min.X),
                                Math.Min(combinedBox.Min.Y, bb.Min.Y),
                                Math.Min(combinedBox.Min.Z, bb.Min.Z));
                            combinedBox.Max = new XYZ(
                                Math.Max(combinedBox.Max.X, bb.Max.X),
                                Math.Max(combinedBox.Max.Y, bb.Max.Y),
                                Math.Max(combinedBox.Max.Z, bb.Max.Z));
                        }
                    }

                    if (combinedBox != null)
                    {
                        if (newView is ViewPlan planView)
                        {
                            // Planview → cropbox
                            planView.CropBoxActive = true;
                            planView.CropBoxVisible = false;
                            planView.CropBox = combinedBox;
                        }
                        else if (newView is View3D view3D)
                        {
                            // Schakel eventueel view template uit
                            view3D.ViewTemplateId = ElementId.InvalidElementId;

                            // Activeer section box
                            view3D.IsSectionBoxActive = true;
                            view3D.SetSectionBox(combinedBox);

                            // Midden van prefab
                            XYZ center = (combinedBox.Min + combinedBox.Max) / 2;

                            // Kijk vanuit top-front-right hoek
                            XYZ eyeDir = new XYZ(1, 1, 1).Normalize();

                            // Plaats camera iets buiten de prefab (5 meter)
                            XYZ eyePosition = center + eyeDir.Multiply(5);

                            // Forward (kijkrichting)
                            XYZ forward = (center - eyePosition).Normalize();

                            // Bepaal een up vector die loodrecht is op forward
                            // Gebruik cross-product met een wereld-as die niet evenwijdig is aan forward
                            XYZ tempUp = new XYZ(0, 0, 1);
                            if (Math.Abs(forward.DotProduct(tempUp)) > 0.99) // bijna parallel
                                tempUp = new XYZ(0, 1, 0);

                            XYZ right = forward.CrossProduct(tempUp).Normalize();
                            XYZ up = right.CrossProduct(forward).Normalize();

                            // Stel oriëntatie in
                            ViewOrientation3D orientation = new ViewOrientation3D(
                                eyePosition,
                                up,
                                forward
                            );

                            view3D.SetOrientation(orientation);
                            view3D.SaveOrientationAndLock();
                            
                            // Zoek en koppel de juiste view template (bijv. "P52_Riolering_3D")
                            string templateSearchName = $"{templateName}_3D";

                            View templateView = new FilteredElementCollector(doc)
                                .OfClass(typeof(View))
                                .Cast<View>()
                                .FirstOrDefault(v => v.IsTemplate && 
                                                     v.Name.Equals(templateSearchName, StringComparison.OrdinalIgnoreCase));

                            if (templateView != null)
                            {
                                view3D.ViewTemplateId = templateView.Id;
                            }
                        }
                    }
                    
                    createdViews.Add(newView);
                }

                // Maak sheet aan
                ElementId titleblockId = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsElementType()
                    .Cast<ElementType>()
                    .FirstOrDefault(e =>
                        e.FamilyName.Equals("Title_Block_Nijhof_inc_Frame_Totaal", StringComparison.OrdinalIgnoreCase) &&
                        e.Name.Equals("A2 - Landscape/Portret", StringComparison.OrdinalIgnoreCase))
                    ?.Id ?? ElementId.InvalidElementId;

                if (titleblockId == ElementId.InvalidElementId)
                {
                    TaskDialog.Show("Prefab", "Geen titleblock gevonden. Sheet wordt niet aangemaakt.");
                    return;
                }

                // Bepaal systeemcode voor tekeningnaam
                string systeemCode = systeem switch
                {
                    "M524" => "HWA",
                    "M521" => "VWA",
                    "M570" => "MV",
                    _ => systeem
                };

                // Sheetnaam zonder scheidingstekens, bijv. PrefabHWAKavel12
                string sheetName = $"Prefab {systeemCode} {kavel}";

                // Simpel oplopend nummer (voor nu P_00, P_01, P_02 ...)
                int sheetCount = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Count();

                string sheetNumber = $"P_{sheetCount:D2}";

                ViewSheet sheet = ViewSheet.Create(doc, titleblockId);
                sheet.Name = sheetName;
                sheet.SheetNumber = sheetNumber;
                
                // Stel parameter 'Tekening Status' in op 'Prefab'
                Parameter statusParam = sheet.LookupParameter("Tekening Status");
                if (statusParam != null && !statusParam.IsReadOnly)
                {
                    statusParam.Set("Prefab");
                }

                // Voeg de views toe
                XYZ insertPoint = new XYZ(0.2, 0.2, 0);
                foreach (View view in createdViews)
                {
                    try
                    {
                        Viewport.Create(doc, sheet.Id, view.Id, insertPoint);
                        insertPoint = new XYZ(insertPoint.X + 0.3, insertPoint.Y, 0);
                    }
                    catch
                    {
                        // 3D-views kunnen niet op een sheet → overslaan
                    }
                }

                TaskDialog dialog = new TaskDialog("Prefab")
                {
                    MainInstruction = $"Sheet '{sheetName}' aangemaakt met {createdViews.Count} views.",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation
                };

                dialog.CommonButtons = TaskDialogCommonButtons.Ok;
                dialog.DefaultButton = TaskDialogResult.Ok;
                dialog.Show();

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.RollBack();
                TaskDialog.Show("Prefab - Fout", ex.Message);
            }
        }
    }

    public string GetName() => "Prefab Create Sheets & Views";
}
