using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace NijhofPanel.Commands.Tools
{
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
                    // Selecteer meerdere objecten
                    IList<Reference> selectedObjects = uidoc.Selection.PickObjects(
                        ObjectType.Element,
                        "Selecteer objecten om prefab gegevens te resetten");

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
                        // Reset 'Prefab Set'
                        Parameter prefabSetParam = element.LookupParameter("Prefab Set");
                        if (prefabSetParam != null && prefabSetParam.StorageType == StorageType.String)
                        {
                            prefabSetParam.Set(string.Empty);
                        }

                        // Reset 'Prefab Color ID'
                        Parameter prefabColorIDParam = element.LookupParameter("Prefab Color ID");
                        if (prefabColorIDParam != null && prefabColorIDParam.StorageType == StorageType.String)
                        {
                            prefabColorIDParam.Set(string.Empty);
                        }

                        // Reset 'Prefab Number'
                        Parameter prefabNumberParam = element.LookupParameter("Prefab Number");
                        if (prefabNumberParam != null && prefabNumberParam.StorageType == StorageType.String)
                        {
                            prefabNumberParam.Set(string.Empty);
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

        public string GetName()
        {
            return "Prefab Set Remove";
        }
    }
}