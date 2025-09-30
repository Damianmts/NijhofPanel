namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class Com_PrefabCreate : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;

        using (Transaction transaction = new Transaction(doc, "Prefab Set Assign"))
        {
            transaction.Start();

            try
            {
                // Selecteer meerdere objecten
                IList<Reference> selectedObjects = uidoc.Selection.PickObjects(ObjectType.Element, "Selecteer objecten");
                if (selectedObjects == null || selectedObjects.Count == 0)
                {
                    TaskDialog.Show("Prefab Set", "Geen elementen geselecteerd.");
                    transaction.RollBack();
                    return;
                }

                // Zoek beschikbare 'Prefab Set' nummers en 'Prefab Color ID'
                HashSet<int> existingSetNumbers = GetUsedPrefabSetNumbers(doc);
                int nextAvailableNumber = FindNextAvailableNumber(existingSetNumbers);
                string prefabColorID = GetNextAvailableColorID(nextAvailableNumber);

                // Haal de elementen op en sorteer ze op hun locatie (Y- en X-coördinaten)
                List<Element> sortedElements = selectedObjects
                    .Select(reference => doc.GetElement(reference))
                    .Where(element => element != null)
                    .OrderBy(element => GetElementLocation(element).Y) // Sorteren op Y (van onder naar boven)
                    .ThenBy(element => GetElementLocation(element).X)  // Daarna sorteren op X (links naar rechts)
                    .ToList();

                // Begin nummering voor elk element binnen de prefab set
                int prefabElementNumber = 1;

                // Verwerk de selectie en nummer alleen diep geneste elementen en normale elementen zonder geneste componenten
                foreach (Element element in sortedElements)
                {
                    if (element is FamilyInstance familyInstance && familyInstance.GetSubComponentIds().Count > 0)
                    {
                        // Nummer alleen de diep geneste elementen
                        NummerGenesteElementen(doc, familyInstance, nextAvailableNumber, prefabColorID, ref prefabElementNumber);
                    }
                    else
                    {
                        // Nummer het element zelf als het geen geneste elementen bevat
                        AssignPrefabParameters(element, nextAvailableNumber, prefabColorID, ref prefabElementNumber);
                    }
                }

                transaction.Commit();

                // Toon één melding met het toegewezen 'Prefab Set' nummer en 'Prefab Color ID'
                TaskDialog.Show("Prefab Set", $"Prefab Set {nextAvailableNumber} met Prefab Color ID {prefabColorID} is aangemaakt.");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                transaction.RollBack();
                TaskDialog.Show("Prefab Set", "Selectie geannuleerd.");
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Prefab Set - Fout", ex.Message);
            }
        }
    }

    public string GetName() => "Prefab Set Assign";

    // Methode om prefab parameters toe te wijzen aan een element
    private void AssignPrefabParameters(Element element, int prefabSetNumber, string prefabColorID, ref int prefabElementNumber)
    {
        // Wijs 'Prefab Set' toe
        Parameter prefabSetParam = element.LookupParameter("Prefab Set");
        if (prefabSetParam?.StorageType == StorageType.String)
        {
            prefabSetParam.Set(prefabSetNumber.ToString());
        }

        // Wijs 'Prefab Color ID' toe
        Parameter prefabColorIDParam = element.LookupParameter("Prefab Color ID");
        if (prefabColorIDParam?.StorageType == StorageType.String)
        {
            prefabColorIDParam.Set(prefabColorID);
        }

        // Wijs een uniek 'Prefab Number' toe binnen de set
        Parameter prefabNumberParam = element.LookupParameter("Prefab Number");
        if (prefabNumberParam?.StorageType == StorageType.String)
        {
            prefabNumberParam.Set(prefabElementNumber.ToString());
            prefabElementNumber++; // Verhoog voor elk nieuw element
        }
    }

    // Methode om geneste elementen te nummeren, inclusief diep geneste elementen
    private void NummerGenesteElementen(Document doc, FamilyInstance familyInstance, int prefabSetNumber, string prefabColorID, ref int prefabElementNumber)
    {
        foreach (ElementId nestedElementId in familyInstance.GetSubComponentIds())
        {
            Element nestedElement = doc.GetElement(nestedElementId);
            if (nestedElement != null)
            {
                AssignPrefabParameters(nestedElement, prefabSetNumber, prefabColorID, ref prefabElementNumber);

                // Controleer of het geneste element zelf ook geneste elementen bevat
                if (nestedElement is FamilyInstance nestedFamilyInstance)
                {
                    NummerGenesteElementen(doc, nestedFamilyInstance, prefabSetNumber, prefabColorID, ref prefabElementNumber);
                }
            }
        }
    }

    private HashSet<int> GetUsedPrefabSetNumbers(Document doc)
    {
        HashSet<int> usedNumbers = new HashSet<int>();
        FilteredElementCollector collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

        foreach (Element element in collector)
        {
            Parameter prefabSetParam = element.LookupParameter("Prefab Set");
            if (prefabSetParam?.StorageType == StorageType.String && int.TryParse(prefabSetParam.AsString(), out int setValue) && setValue > 0)
            {
                usedNumbers.Add(setValue);
            }
        }

        return usedNumbers;
    }

    private int FindNextAvailableNumber(HashSet<int> existingSetNumbers)
    {
        int number = 1;
        while (existingSetNumbers.Contains(number))
        {
            number++;
        }
        return number;
    }

    private string GetNextAvailableColorID(int prefabSetNumber)
    {
        int colorID = (prefabSetNumber - 1) % 10 + 1;
        return colorID.ToString("D2"); // Zorg ervoor dat het een twee-cijferige string is, bv. "01"
    }

    private XYZ GetElementLocation(Element element)
    {
        Location location = element.Location;
        if (location is LocationPoint pointLocation)
        {
            return pointLocation.Point;
        }
        else if (location is LocationCurve curveLocation)
        {
            return curveLocation.Curve.GetEndPoint(0); // Startpunt van de curve
        }
        return XYZ.Zero;
    }
}