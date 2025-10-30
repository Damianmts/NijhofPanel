namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

public class Com_PrefabDelete : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;

        using (Transaction transaction = new Transaction(doc, "Prefab Set Remove"))
        {
            transaction.Start();

            try
            {
                IList<Reference> selectedObjects;

                // 🔹 Controleer of er al elementen geselecteerd zijn in Revit
                ICollection<ElementId> preSelectedIds = uidoc.Selection.GetElementIds();

                if (preSelectedIds != null && preSelectedIds.Count > 0)
                {
                    selectedObjects = preSelectedIds
                        .Select(id => new Reference(doc.GetElement(id)))
                        .ToList();
                }
                else
                {
                    // Anders vraag gebruiker om selectie te maken
                    selectedObjects = uidoc.Selection.PickObjects(
                        ObjectType.Element,
                        "Selecteer objecten om prefab gegevens te resetten");
                }

                if (selectedObjects == null || selectedObjects.Count == 0)
                {
                    TaskDialog.Show("Prefab Delete", "Geen elementen geselecteerd.");
                    transaction.RollBack();
                    return;
                }

                // Haal de geselecteerde elementen op
                List<Element> elementsToClear = selectedObjects
                    .Select(reference => doc.GetElement(reference))
                    .Where(element => element != null)
                    .ToList();

                foreach (Element element in elementsToClear)
                {
                    // Reset Prefab Set
                    Parameter prefabSetParam = element.LookupParameter("Prefab Set");
                    if (prefabSetParam != null && prefabSetParam.StorageType == StorageType.String)
                        prefabSetParam.Set(string.Empty);

                    // Reset Prefab Color ID
                    Parameter prefabColorIDParam = element.LookupParameter("Prefab Color ID");
                    if (prefabColorIDParam != null && prefabColorIDParam.StorageType == StorageType.String)
                        prefabColorIDParam.Set(string.Empty);

                    // Reset Prefab Number
                    Parameter prefabNumberParam = element.LookupParameter("Prefab Number");
                    if (prefabNumberParam != null && prefabNumberParam.StorageType == StorageType.String)
                        prefabNumberParam.Set(string.Empty);

                    // Reset Prefab Kavelnummer of Kavelnummer
                    Parameter? kavelParam = element.LookupParameter("Prefab Kavelnummer") ??
                                            element.LookupParameter("Kavelnummer");
                    if (kavelParam != null && kavelParam.StorageType == StorageType.String)
                        kavelParam.Set(string.Empty);

                    // Reset Prefab Verdieping of Verdieping
                    Parameter? verdiepingParam = element.LookupParameter("Prefab Verdieping") ??
                                                 element.LookupParameter("Verdieping");
                    if (verdiepingParam != null && verdiepingParam.StorageType == StorageType.String)
                        verdiepingParam.Set(string.Empty);

                    // Controleer op nested families en reset ook die
                    if (element is FamilyInstance familyInstance && familyInstance.GetSubComponentIds().Count > 0)
                    {
                        ClearPrefabParametersFromNestedFamilies(doc, familyInstance);
                    }

                    // Laat 'Manufacturer Art. No.' intact
                }

                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Selectie geannuleerd
                transaction.RollBack();
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Prefab Delete", ex.Message);
            }
        }
    }

    /// <summary>
    /// Recursieve functie om prefabparameters te verwijderen uit alle geneste families.
    /// </summary>
    private void ClearPrefabParametersFromNestedFamilies(Document doc, FamilyInstance parentInstance)
    {
        ICollection<ElementId> subComponentIds = parentInstance.GetSubComponentIds();

        foreach (ElementId subId in subComponentIds)
        {
            Element subElement = doc.GetElement(subId);
            if (subElement == null)
                continue;

            // Reset Prefab Set
            Parameter prefabSetParam = subElement.LookupParameter("Prefab Set");
            if (prefabSetParam != null && prefabSetParam.StorageType == StorageType.String)
                prefabSetParam.Set(string.Empty);

            // Reset Prefab Color ID
            Parameter prefabColorIDParam = subElement.LookupParameter("Prefab Color ID");
            if (prefabColorIDParam != null && prefabColorIDParam.StorageType == StorageType.String)
                prefabColorIDParam.Set(string.Empty);

            // Reset Prefab Number
            Parameter prefabNumberParam = subElement.LookupParameter("Prefab Number");
            if (prefabNumberParam != null && prefabNumberParam.StorageType == StorageType.String)
                prefabNumberParam.Set(string.Empty);

            // Reset Prefab Kavelnummer of Kavelnummer
            Parameter? kavelParam = subElement.LookupParameter("Prefab Kavelnummer") ??
                                    subElement.LookupParameter("Kavelnummer");
            if (kavelParam != null && kavelParam.StorageType == StorageType.String)
                kavelParam.Set(string.Empty);

            // Reset Prefab Verdieping of Verdieping
            Parameter? verdiepingParam = subElement.LookupParameter("Prefab Verdieping") ??
                                         subElement.LookupParameter("Verdieping");
            if (verdiepingParam != null && verdiepingParam.StorageType == StorageType.String)
                verdiepingParam.Set(string.Empty);

            // Recursief verder voor dieper geneste families
            if (subElement is FamilyInstance nestedInstance && nestedInstance.GetSubComponentIds().Count > 0)
                ClearPrefabParametersFromNestedFamilies(doc, nestedInstance);
        }
    }

    public string GetName()
    {
        return "Prefab Set Remove";
    }
}