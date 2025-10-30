namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

public class Com_PrefabAdd : IExternalEventHandler
    {
        public string GetName() => "NijhofPanel - Prefab parameters kopiëren";

        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 🔹 Stap 1: Kies eerst een bron-element (de prefabset waarvan de data gekopieerd wordt)
                Reference sourceRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    "Selecteer een element met de gewenste prefabgegevens");

                if (sourceRef == null)
                    return;

                Element sourceElement = doc.GetElement(sourceRef);
                if (sourceElement == null)
                    return;

                // Lees de prefabparameters uit het bron-element
                string prefabSet = GetParameterValue(sourceElement, "Prefab Set");
                string prefabColorID = GetParameterValue(sourceElement, "Prefab Color ID");
                string prefabKavel = GetParameterValue(sourceElement, "Prefab Kavelnummer") ??
                                     GetParameterValue(sourceElement, "Kavelnummer");
                string prefabVerdieping = GetParameterValue(sourceElement, "Prefab Verdieping") ??
                                          GetParameterValue(sourceElement, "Verdieping");

                if (string.IsNullOrWhiteSpace(prefabSet) || string.IsNullOrWhiteSpace(prefabColorID))
                {
                    TaskDialog.Show("Prefab Kopiëren", "Het geselecteerde element bevat geen prefabgegevens.");
                    return;
                }

                // 🔹 Stap 2: Kies nu de doel-elementen (waar de prefabdata naartoe moet)
                IList<Reference> targetRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    "Selecteer de elementen waaraan de prefabgegevens moeten worden toegewezen");

                if (targetRefs == null || targetRefs.Count == 0)
                {
                    TaskDialog.Show("Prefab Kopiëren", "Geen doel-elementen geselecteerd.");
                    return;
                }

                List<Element> targetElements = targetRefs
                    .Select(r => doc.GetElement(r))
                    .Where(e => e != null)
                    .ToList();

                // 🔹 Stap 3: Start transactie en kopieer de parameters
                using (Transaction trans = new Transaction(doc, "Prefab gegevens toewijzen"))
                {
                    trans.Start();

                    foreach (Element element in targetElements)
                    {
                        // Kopieer prefabparameters naar het hoofdelement
                        SetParameterValue(element, "Prefab Set", prefabSet);
                        SetParameterValue(element, "Prefab Color ID", prefabColorID);
                        SetParameterValue(element, "Prefab Kavelnummer", prefabKavel);
                        SetParameterValue(element, "Kavelnummer", prefabKavel);
                        SetParameterValue(element, "Prefab Verdieping", prefabVerdieping);
                        SetParameterValue(element, "Verdieping", prefabVerdieping);

                        // Recursief voor nested families
                        if (element is FamilyInstance familyInstance && familyInstance.GetSubComponentIds().Count > 0)
                        {
                            AssignParametersToNestedFamilies(doc, familyInstance, prefabSet, prefabColorID, prefabKavel, prefabVerdieping);
                        }
                    }

                    trans.Commit();
                }

                TaskDialog.Show("Prefab Kopiëren", "Prefabgegevens succesvol toegewezen aan de geselecteerde elementen.");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Selectie geannuleerd
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Prefab Kopiëren - Fout", ex.Message);
            }
        }

        // 🧩 Hulpfunctie: Parameterwaarde ophalen
        private string GetParameterValue(Element element, string paramName)
        {
            Parameter param = element.LookupParameter(paramName);
            if (param != null && param.HasValue && param.StorageType == StorageType.String)
                return param.AsString();
            return null!;
        }

        // 🧩 Hulpfunctie: Parameterwaarde instellen
        private void SetParameterValue(Element element, string paramName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            Parameter param = element.LookupParameter(paramName);
            if (param != null && !param.IsReadOnly && param.StorageType == StorageType.String)
                param.Set(value);
        }

        // 🧩 Recursieve functie om prefabparameters toe te wijzen aan alle geneste families
        private void AssignParametersToNestedFamilies(Document doc, FamilyInstance parentInstance,
            string prefabSet, string prefabColorID, string prefabKavel, string prefabVerdieping)
        {
            ICollection<ElementId> subComponentIds = parentInstance.GetSubComponentIds();

            foreach (ElementId subId in subComponentIds)
            {
                Element subElement = doc.GetElement(subId);
                if (subElement == null)
                    continue;

                SetParameterValue(subElement, "Prefab Set", prefabSet);
                SetParameterValue(subElement, "Prefab Color ID", prefabColorID);
                SetParameterValue(subElement, "Prefab Kavelnummer", prefabKavel);
                SetParameterValue(subElement, "Kavelnummer", prefabKavel);
                SetParameterValue(subElement, "Prefab Verdieping", prefabVerdieping);
                SetParameterValue(subElement, "Verdieping", prefabVerdieping);

                // Recursief doorgaan voor dieper geneste families
                if (subElement is FamilyInstance nestedInstance && nestedInstance.GetSubComponentIds().Count > 0)
                {
                    AssignParametersToNestedFamilies(doc, nestedInstance, prefabSet, prefabColorID, prefabKavel, prefabVerdieping);
                }
            }
        }
    }