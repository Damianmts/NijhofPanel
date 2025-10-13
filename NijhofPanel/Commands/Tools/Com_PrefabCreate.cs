namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using ViewModels;
using Autodesk.Revit.DB.Plumbing;
using System.Text.RegularExpressions;
using NijhofPanel.Helpers.Core;

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
                IList<Reference> selectedReferences;

                // Controleer of er al elementen geselecteerd zijn
                var preSelectedIds = uidoc.Selection.GetElementIds();

                if (preSelectedIds != null && preSelectedIds.Count > 0)
                {
                    // Gebruik bestaande selectie
                    selectedReferences = preSelectedIds
                        .Select(id => new Reference(doc.GetElement(id)))
                        .ToList();
                }
                else
                {
                    // Vraag gebruiker om elementen te selecteren
                    selectedReferences = uidoc.Selection.PickObjects(ObjectType.Element, "Selecteer objecten");
                }

                if (selectedReferences == null || selectedReferences.Count == 0)
                {
                    TaskDialog.Show("Prefab Set", "Geen elementen geselecteerd.");
                    transaction.RollBack();
                    return;
                }

                // Zoek beschikbare 'Prefab Set' nummers en 'Prefab Color ID'
                HashSet<int> existingSetNumbers = GetUsedPrefabSetNumbers(doc);
                int nextAvailableNumber = FindNextAvailableNumber(existingSetNumbers);
                string prefabColorID = GetNextAvailableColorID(nextAvailableNumber);

                // Haal de geselecteerde elementen op
                List<Element> selectedElements = selectedReferences
                    .Select(reference => doc.GetElement(reference))
                    .Where(element => element != null)
                    .ToList();

                // Wijs prefab parameters toe aan alle geselecteerde elementen
                foreach (Element element in selectedElements)
                {
                    if (element is FamilyInstance familyInstance && familyInstance.GetSubComponentIds().Count > 0)
                    {
                        AssignParametersToNestedFamilies(doc, familyInstance, nextAvailableNumber.ToString(), prefabColorID);
                    }
                    else
                    {
                        AssignPrefabParameters(element, nextAvailableNumber, prefabColorID);
                    }
                }

                // Vraag gebruiker om invoer: BNR, Kavel of Type
                string? kavelnummer = InputBoxHelper.Show(
                    "Voer het Prefab kenmerk in (bijv. 'BNR 12', 'Kavel 5' of 'Type A2'):",
                    "Prefab Kenmerk"
                );

                // Controleer of invoer geldig is
                if (string.IsNullOrWhiteSpace(kavelnummer))
                {
                    TaskDialog.Show("Prefab Set", "Geen invoer gedaan. De actie is geannuleerd.");
                    transaction.RollBack();
                    return;
                }

                // Sta 'BNR', 'Kavel' of 'Type' toe
                if (!Regex.IsMatch(kavelnummer, @"^(BNR|Kavel|Type)\s+\S+$", RegexOptions.IgnoreCase))
                {
                    TaskDialog.Show("Prefab Set",
                        "Ongeldige invoer. Gebruik 'BNR [nummer]', 'Kavel [nummer]' of 'Type [code]'.");
                    transaction.RollBack();
                    return;
                }
                
                // Vraag gebruiker om Prefab Verdieping
                string? verdieping = InputBoxHelper.Show(
                    "Voer de Prefab Verdieping in (bijv. '-V01', 'V00', 'V01', etc.):",
                    "Prefab Verdieping"
                );

                // Controleer of invoer geldig is
                if (string.IsNullOrWhiteSpace(verdieping))
                {
                    TaskDialog.Show("Prefab Set", "Geen verdieping ingevuld. De actie is geannuleerd.");
                    transaction.RollBack();
                    return;
                }
                
                // Wijs het kavelnummer toe aan alle geselecteerde elementen (ook geneste)
                foreach (Element element in selectedElements)
                {
                    AssignPrefabKavelnummer(element, kavelnummer);
                    AssignPrefabVerdieping(element, verdieping);

                    if (element is FamilyInstance familyInstance && familyInstance.GetSubComponentIds().Count > 0)
                    {
                        AssignParametersToNestedFamilies(doc, familyInstance, nextAvailableNumber.ToString(), prefabColorID, kavelnummer, verdieping);
                    }
                }
                
                transaction.Commit();

                // Toon één melding met het toegewezen 'Prefab Set' nummer en 'Prefab Color ID'
                // Toon bevestigingsvenster met Yes/No
                TaskDialogResult result = TaskDialog.Show(
                    "Prefab Set",
                    $"Prefab Set {nextAvailableNumber} met Prefab Color ID {prefabColorID} is aangemaakt.\n\n" +
                    "Wil je de views en sheet aanmaken?",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                if (result == TaskDialogResult.Yes)
                {
                    try
                    {
                        // Roep nieuwe class aan om sheets en views aan te maken
                        var command = new Com_PrefabCreateSheetsAndViews(nextAvailableNumber.ToString());
                        command.Execute(app);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Prefab Set - Fout", $"Fout bij aanmaken sheets/views:\n{ex.Message}");
                    }
                }
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
    private void AssignPrefabParameters(Element element, int prefabSetNumber, string prefabColorID)
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

        // Wijs artikelnummer toe als het een pipe is
        AssignArticleNumberToPipe(element);
    }

    // Methode om artikelnummer toe te wijzen aan een pipe
    private void AssignArticleNumberToPipe(Element element)
    {
        // Controleer of het element een Pipe is
        if (!(element is Pipe pipe))
            return;

        try
        {
            // Haal Pipe Type Name op (bijvoorbeeld "Dyka PVC - 110mm")
            ElementId typeId = pipe.GetTypeId();
            Element pipeType = pipe.Document.GetElement(typeId);
            string pipeTypeName = pipeType?.Name ?? "";

            // Haal Family Name op als backup
            string familyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString() ?? "";

            // Haal System Abbreviation op
            Parameter systemAbbrevParam = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
            string systemAbbreviation = systemAbbrevParam?.AsString() ?? "";

            // Haal Diameter op (in millimeters)
            Parameter diameterParam = element.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (diameterParam == null)
                return;

            double diameterInFeet = diameterParam.AsDouble();
            double diameterInMM = UnitUtils.ConvertFromInternalUnits(diameterInFeet, UnitTypeId.Millimeters);
            string diameter = Math.Round(diameterInMM).ToString();

            // Bepaal het producttype op basis van Pipe Type Name en System Abbreviation
            string productType = DetermineProductType(pipeTypeName, familyName, systemAbbreviation, diameter);

            if (string.IsNullOrEmpty(productType))
                return;

            // Haal het artikelnummer op
            string artikelnummer = SettingsPageViewModel.GetArtikelnummer(productType, diameter);

            if (string.IsNullOrEmpty(artikelnummer))
                return;

            // Probeer eerst NLRS_C_code_product in te vullen
            Parameter nlrsParam = element.LookupParameter("NLRS_C_code_product");
            if (nlrsParam != null && !nlrsParam.IsReadOnly)
            {
                nlrsParam.Set(artikelnummer);
            }
            else
            {
                // Als backup, probeer Manufacturer Art. No
                Parameter manufacturerParam = element.LookupParameter("Manufacturer Art. No.");
                if (manufacturerParam != null && !manufacturerParam.IsReadOnly)
                {
                    manufacturerParam.Set(artikelnummer);
                }
            }
        }
        catch (Exception)
        {
            // Negeer fouten bij individuele pipes
        }
    }

    // Methode om het producttype te bepalen op basis van Pipe Type Name en System Abbreviation
    private string DetermineProductType(string pipeTypeName, string familyName, string systemAbbreviation,
        string diameter)
    {
        // Normaliseer de strings (lowercase en trim)
        pipeTypeName = pipeTypeName?.ToLower().Trim() ?? "";
        familyName = familyName?.ToLower().Trim() ?? "";
        systemAbbreviation = systemAbbreviation?.ToLower().Trim() ?? "";

        // Check of het HWA is (via pipe type naam of system abbreviation)
        bool isHWA = (pipeTypeName.Contains("hwa") ||
                      pipeTypeName.Contains("dyka") && systemAbbreviation.Contains("m521"));

        if (isHWA)
        {
            // Voor HWA: alleen 80mm uit HWA lijst, rest uit PVC lijst
            if (diameter == "80")
            {
                return "DykaHWA";
            }
            else
            {
                return "DykaPVC";
            }
        }

        // Dyka Sono - voor geluidsisolatie
        if (pipeTypeName.Contains("sono") || familyName.Contains("sono") && systemAbbreviation.Contains("m524"))
        {
            return "DykaSono";
        }

        // Dyka PVC - voor sanitair/vuilwater
        if (pipeTypeName.Contains("pvc") || pipeTypeName.Contains("dyka") ||
            familyName.Contains("dyka") && systemAbbreviation.Contains("m524"))
        {
            return "DykaPVC";
        }

        // Dyka Air - voor ventilatie
        if (pipeTypeName.Contains("air") || pipeTypeName.Contains("lucht") ||
            systemAbbreviation.Contains("air") || systemAbbreviation.Contains("vent") ||
            systemAbbreviation.Contains("lucht"))
        {
            return "DykaAir";
        }

        return null;
    }

    // Functie om parameters toe te wijzen aan alle nested families (recursief)
    private void AssignParametersToNestedFamilies(Document doc, FamilyInstance parentInstance, string prefabSet,
        string prefabColorID, string kavelnummer = null, string verdieping = null)
    {
        // Haal alle sub-componenten (nested families) op
        ICollection<ElementId> subComponentIds = parentInstance.GetSubComponentIds();

        foreach (ElementId subId in subComponentIds)
        {
            Element subElement = doc.GetElement(subId);

            if (subElement != null)
            {
                // Wijs parameters toe aan de nested family
                Parameter prefabSetParam = subElement.LookupParameter("Prefab Set");
                Parameter prefabColorIDParam = subElement.LookupParameter("Prefab Color ID");
                Parameter? kavelParam = subElement.LookupParameter("Prefab Kavelnummer") ?? subElement.LookupParameter("Kavelnummer");
                Parameter? verdiepingParam = subElement.LookupParameter("Prefab Verdieping") ?? subElement.LookupParameter("Verdieping");

                if (prefabSetParam != null && prefabSetParam.StorageType == StorageType.String)
                    prefabSetParam.Set(prefabSet);

                if (prefabColorIDParam != null && prefabColorIDParam.StorageType == StorageType.String)
                    prefabColorIDParam.Set(prefabColorID);

                if (!string.IsNullOrEmpty(kavelnummer) && kavelParam != null && kavelParam.StorageType == StorageType.String && !kavelParam.IsReadOnly)
                    kavelParam.Set(kavelnummer);
                
                if (!string.IsNullOrEmpty(verdieping) && verdiepingParam != null && verdiepingParam.StorageType == StorageType.String && !verdiepingParam.IsReadOnly)
                    verdiepingParam.Set(verdieping);
                

                // Wijs artikelnummer toe als het een pipe is
                AssignArticleNumberToPipe(subElement);

                // Als dit sub-element ook een FamilyInstance is, ga dan recursief door
                if (subElement is FamilyInstance nestedFamilyInstance)
                {
                    AssignParametersToNestedFamilies(doc, nestedFamilyInstance, prefabSet, prefabColorID, kavelnummer, verdieping);
                }
            }
        }
    }

    private void AssignPrefabKavelnummer(Element element, string kavelnummer)
    {
        Parameter? kavelParam =
            element.LookupParameter("Prefab Kavelnummer") ??
            element.LookupParameter("Kavelnummer");

        if (kavelParam != null &&
            kavelParam.StorageType == StorageType.String &&
            !kavelParam.IsReadOnly)
        {
            kavelParam.Set(kavelnummer);
        }
    }
    
    private void AssignPrefabVerdieping(Element element, string verdieping)
    {
        Parameter? verdiepingParam =
            element.LookupParameter("Prefab Verdieping") ??
            element.LookupParameter("Verdieping");

        if (verdiepingParam != null &&
            verdiepingParam.StorageType == StorageType.String &&
            !verdiepingParam.IsReadOnly)
        {
            verdiepingParam.Set(verdieping);
        }
    }
    
    private HashSet<int> GetUsedPrefabSetNumbers(Document doc)
    {
        HashSet<int> usedNumbers = new HashSet<int>();
        FilteredElementCollector collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

        foreach (Element element in collector)
        {
            Parameter prefabSetParam = element.LookupParameter("Prefab Set");
            if (prefabSetParam?.StorageType == StorageType.String &&
                int.TryParse(prefabSetParam.AsString(), out int setValue) && setValue > 0)
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
}