namespace NijhofPanel.Commands.Tools;

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

/// <summary>
/// External Event Handler voor het taggen van VWA (Vuilwaterafvoer) lengtes met 2.5mm tag
/// </summary>
public class Com_TagVWALength : IExternalEventHandler
{
    private const string TAG_FAMILY_NAME = "M_Pipe_Tag_Nijhof";
    private const string TAG_TYPE_NAME = "Length 2.5 mm";
    private const string SYSTEM_CLASSIFICATION = "Sanitary";
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
            FamilySymbol selectedPipeTag = GetPipeTagFamilySymbol(doc);
            if (selectedPipeTag == null)
                return;

            // Voer de tagging operatie uit
            ExecuteTaggingOperation(doc, activeView, selectedPipeTag);

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

    public string GetName() => "Tag VWA Length";
    
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
                return null!;
            }
            
            return viewport;
        }
        catch (OperationCanceledException)
        {
            return null!;
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
            return null!;
        }
        
        return activeView;
    }

    /// <summary>
    /// Verzamelt en filtert sanitary pipes in de view
    /// </summary>
    private List<Element> CollectSanitaryPipes(Document doc, ElementId viewId)
    {
        return new FilteredElementCollector(doc, viewId)
            .OfCategory(BuiltInCategory.OST_PipeCurves)
            .WhereElementIsNotElementType()
            .WherePasses(new ElementClassFilter(typeof(Pipe)))
            .WherePasses(new ElementParameterFilter(
                new FilterStringRule(
                    new ParameterValueProvider(new ElementId(BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM)),
                    new FilterStringEquals(),
                    SYSTEM_CLASSIFICATION)))
            .ToElements()
            .ToList();
    }

    /// <summary>
    /// Scheidt pipes in verticale (te verbergen) en horizontale (te taggen) pipes
    /// </summary>
    private void CategorizePipes(
        List<Element> pipes, 
        out List<ElementId> elementsToHide, 
        out List<Element> elementsToBeTagged)
    {
        elementsToHide = new List<ElementId>();
        elementsToBeTagged = new List<Element>();

        foreach (Pipe pipe in pipes.Cast<Pipe>())
        {
            if (IsPipeVertical(pipe))
            {
                elementsToHide.Add(pipe.Id);
            }
            else
            {
                elementsToBeTagged.Add(pipe);
            }
        }
    }

    /// <summary>
    /// Controleert of een pipe verticaal is
    /// </summary>
    private bool IsPipeVertical(Pipe pipe)
    {
        if (!(pipe.Location is LocationCurve location))
            return false;

        XYZ direction = (location.Curve.GetEndPoint(1) - location.Curve.GetEndPoint(0)).Normalize();
        
        return Math.Abs(direction.Z) > VERTICAL_THRESHOLD;
    }

    /// <summary>
    /// Verkrijgt de pipe tag family symbol (2.5mm)
    /// </summary>
    private FamilySymbol GetPipeTagFamilySymbol(Document doc)
    {
        FilteredElementCollector tagCollector = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PipeTags);

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

        if (!(filteredTags.FirstOrDefault() is FamilySymbol selectedPipeTag))
        {
            TaskDialog.Show("Error", 
                $"Tag '{TAG_TYPE_NAME}' van familie '{TAG_FAMILY_NAME}' niet gevonden.\nLaad de tag en probeer het opnieuw.");
            return null!;
        }

        return selectedPipeTag;
    }

    /// <summary>
    /// Creëert tags voor de opgegeven pipes
    /// </summary>
    private void CreateTagsForPipes(Document doc, View activeView, FamilySymbol tagSymbol, List<Element> pipes)
    {
        if (!tagSymbol.IsActive)
            tagSymbol.Activate();

        foreach (Element pipe in pipes)
        {
            if (!(pipe.Location is LocationCurve pipeCurve))
                continue;

            XYZ midpoint = (pipeCurve.Curve.GetEndPoint(0) + pipeCurve.Curve.GetEndPoint(1)) / 2;
            XYZ tagPoint = new XYZ(midpoint.X, midpoint.Y, activeView.GenLevel.Elevation);

            IndependentTag.Create(
                doc, 
                tagSymbol.Id, 
                activeView.Id, 
                new Reference(pipe), 
                true, 
                TagOrientation.Horizontal, 
                tagPoint);
        }
    }

    /// <summary>
    /// Voert de volledige tagging operatie uit
    /// </summary>
    private void ExecuteTaggingOperation(Document doc, View activeView, FamilySymbol selectedPipeTag)
    {
        using (Transaction tagTransaction = new Transaction(doc, "NT - VWA Prefab taggen"))
        {
            tagTransaction.Start();

            // Verzamel pipes
            List<Element> filterPipes = CollectSanitaryPipes(doc, activeView.Id);

            if (filterPipes.Count == 0)
            {
                TaskDialog.Show("Info", "Geen sanitary pipes gevonden in de view.");
                tagTransaction.RollBack();
                return;
            }

            // Categoriseer pipes
            CategorizePipes(filterPipes, out List<ElementId> elementsToHide, out List<Element> elementsToBeTagged);

            // Verberg verticale pipes
            if (elementsToHide.Count > 0)
            {
                activeView.HideElements(elementsToHide);
            }

            // Creëer tags voor horizontale pipes
            if (elementsToBeTagged.Count > 0)
            {
                CreateTagsForPipes(doc, activeView, selectedPipeTag, elementsToBeTagged);
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