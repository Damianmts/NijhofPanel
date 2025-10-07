namespace NijhofPanel.Commands.Tools;

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

/// <summary>
/// External Event Handler voor het taggen van MV (Mechanische Ventilatie) ducts met lengtetags
/// </summary>
public class Com_TagMVLength : IExternalEventHandler
{
    private const string TAG_FAMILY_NAME = "VE_Duct_Tag_Nijhof";
    private const string TAG_TYPE_NAME = "Length 2.5 mm";
    private const double VERTICAL_THRESHOLD = 0.8;
    
    public void Execute(UIApplication app)
    {
        try
        {
            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;
            
            Document doc = uidoc.Document;
            View initialActiveView = doc.ActiveView;

            // Valideer dat we in een sheet zijn
            if (!ValidateViewIsSheet(initialActiveView))
                return;

            // Selecteer viewport
            Viewport viewport = SelectViewport(uidoc, doc);
            if (viewport == null)
                return;

            // Valideer en verkrijg de actieve view
            View activeView = ValidateAndGetView(doc, viewport);
            if (activeView == null)
                return;

            // Verkrijg de tag family symbol
            FamilySymbol selectedDuctTag = GetDuctTagFamilySymbol(doc);
            if (selectedDuctTag == null)
                return;

            // Voer de tagging operatie uit
            ExecuteTaggingOperation(doc, activeView, selectedDuctTag);

            // Herstel de initiële view
            RestoreInitialView(uidoc, initialActiveView);
        }
        catch (OperationCanceledException)
        {
            // User cancel: niets tonen
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Tag MV Length";
    
    /// <summary>
    /// Valideert dat de huidige view een sheet is
    /// </summary>
    private bool ValidateViewIsSheet(View view)
    {
        if (!(view is ViewSheet))
        {
            TaskDialog.Show("Error", "Deze functie werkt alleen in een sheet. Zorg ervoor dat je uit de viewport bent.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Laat gebruiker een viewport selecteren
    /// </summary>
    private Viewport SelectViewport(UIDocument uidoc, Document doc)
    {
        try
        {
            Reference reference = uidoc.Selection.PickObject(
                ObjectType.Element, 
                new ViewportSelectionFilter(), 
                "Selecteer een viewport");
            
            Element viewElement = doc.GetElement(reference);
            
            if (!(viewElement is Viewport viewport))
            {
                TaskDialog.Show("Error", "Selecteer een geldige viewport.");
                return null;
            }
            
            return viewport;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Valideert en verkrijgt de view van de viewport
    /// </summary>
    private View ValidateAndGetView(Document doc, Viewport viewport)
    {
        if (!(doc.GetElement(viewport.ViewId) is View activeView) || 
            activeView.ViewType == ViewType.Legend || 
            activeView.ViewType == ViewType.ThreeD)
        {
            TaskDialog.Show("Error", "Selecteer een geschikte floorplan.");
            return null;
        }
        
        return activeView;
    }

    /// <summary>
    /// Verzamelt alle ducts in de view
    /// </summary>
    private List<Element> CollectDucts(Document doc, ElementId viewId)
    {
        return new FilteredElementCollector(doc, viewId)
            .OfCategory(BuiltInCategory.OST_DuctCurves)
            .WhereElementIsNotElementType()
            .WherePasses(new ElementClassFilter(typeof(Duct)))
            .ToElements()
            .ToList();
    }

    /// <summary>
    /// Scheidt ducts in verticale (te verbergen) en horizontale (te taggen) ducts
    /// </summary>
    private void CategorizeDucts(
        List<Element> ducts, 
        out List<ElementId> elementsToHide, 
        out List<Element> elementsToBeTagged)
    {
        elementsToHide = new List<ElementId>();
        elementsToBeTagged = new List<Element>();

        foreach (Duct duct in ducts.Cast<Duct>())
        {
            if (IsDuctVertical(duct))
            {
                elementsToHide.Add(duct.Id);
            }
            else
            {
                elementsToBeTagged.Add(duct);
            }
        }
    }

    /// <summary>
    /// Controleert of een duct verticaal is
    /// </summary>
    private bool IsDuctVertical(Duct duct)
    {
        if (!(duct.Location is LocationCurve location))
            return false;

        XYZ direction = (location.Curve.GetEndPoint(1) - location.Curve.GetEndPoint(0)).Normalize();
        
        return Math.Abs(direction.Z) > VERTICAL_THRESHOLD;
    }

    /// <summary>
    /// Verkrijgt de duct tag family symbol (2.5mm)
    /// </summary>
    private FamilySymbol GetDuctTagFamilySymbol(Document doc)
    {
        FilteredElementCollector tagCollector = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_DuctTags);

        FilterRule familyRule = ParameterFilterRuleFactory.CreateEqualsRule(
            new ElementId(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM), 
            TAG_FAMILY_NAME);
        ElementParameterFilter familyFilter = new ElementParameterFilter(familyRule);

        FilterRule typeRule = ParameterFilterRuleFactory.CreateEqualsRule(
            new ElementId(BuiltInParameter.SYMBOL_NAME_PARAM), 
            TAG_TYPE_NAME);
        ElementParameterFilter typeFilter = new ElementParameterFilter(typeRule);

        LogicalAndFilter combinedFilter = new LogicalAndFilter(familyFilter, typeFilter);
        List<Element> filteredTags = tagCollector.WherePasses(combinedFilter).ToElements().ToList();

        if (!(filteredTags.FirstOrDefault() is FamilySymbol selectedDuctTag))
        {
            TaskDialog.Show("Error", 
                $"Tag '{TAG_TYPE_NAME}' van familie '{TAG_FAMILY_NAME}' niet gevonden.\nLaad de tag en probeer het opnieuw.");
            return null;
        }

        return selectedDuctTag;
    }

    /// <summary>
    /// Creëert tags voor de opgegeven ducts
    /// </summary>
    private void CreateTagsForDucts(Document doc, View activeView, FamilySymbol tagSymbol, List<Element> ducts)
    {
        if (!tagSymbol.IsActive)
            tagSymbol.Activate();

        foreach (Element duct in ducts)
        {
            if (!(duct.Location is LocationCurve ductCurve))
                continue;

            XYZ midpoint = (ductCurve.Curve.GetEndPoint(0) + ductCurve.Curve.GetEndPoint(1)) / 2;
            XYZ tagPoint = new XYZ(midpoint.X, midpoint.Y, activeView.GenLevel.Elevation);

            IndependentTag.Create(
                doc, 
                tagSymbol.Id, 
                activeView.Id, 
                new Reference(duct), 
                true, 
                TagOrientation.Horizontal, 
                tagPoint);
        }
    }

    /// <summary>
    /// Voert de volledige tagging operatie uit
    /// </summary>
    private void ExecuteTaggingOperation(Document doc, View activeView, FamilySymbol selectedDuctTag)
    {
        using (Transaction tagTransaction = new Transaction(doc, "NT - MV Prefab taggen"))
        {
            tagTransaction.Start();

            // Verzamel ducts
            List<Element> filterDucts = CollectDucts(doc, activeView.Id);

            if (filterDucts.Count == 0)
            {
                TaskDialog.Show("Info", "Geen ducts gevonden in de view.");
                tagTransaction.RollBack();
                return;
            }

            // Categoriseer ducts
            CategorizeDucts(filterDucts, out List<ElementId> elementsToHide, out List<Element> elementsToBeTagged);

            // Verberg verticale ducts
            if (elementsToHide.Count > 0)
            {
                activeView.HideElements(elementsToHide);
            }

            // Creëer tags voor horizontale ducts
            if (elementsToBeTagged.Count > 0)
            {
                CreateTagsForDucts(doc, activeView, selectedDuctTag, elementsToBeTagged);
            }
            else
            {
                tagTransaction.RollBack();
                return;
            }

            tagTransaction.Commit();
        }
    }

    /// <summary>
    /// Herstelt de initiële view
    /// </summary>
    private void RestoreInitialView(UIDocument uidoc, View initialActiveView)
    {
        if (uidoc.ActiveView.Id != initialActiveView.Id)
        {
            uidoc.ActiveView = initialActiveView;
        }
    }
}