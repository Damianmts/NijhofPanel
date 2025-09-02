using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace NijhofPanel.Commands.Tools;

public class Com_PrefabAdd : IExternalEventHandler
{
    // Naam die in Revit kan worden getoond voor logging/debug
    public string GetName() => "NijhofPanel - Prefab parameters toewijzen";

    // Entry-point voor ExternalEvent
    public void Execute(UIApplication app)
    {
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;

        try
        {
            // Stap 1: Selecteer elementen om prefab parameters aan toe te voegen
            IList<Reference> selectedElements = uidoc.Selection.PickObjects(
                ObjectType.Element,
                "Selecteer de elementen om prefab parameters toe te wijzen");

            using (Transaction trans = new Transaction(doc, "Prefab set toewijzen"))
            {
                trans.Start();

                foreach (Reference reference in selectedElements)
                {
                    Element element = doc.GetElement(reference);

                    // Zoek naar verbonden elementen en haal de Prefab Set en Prefab Color ID parameters op
                    string prefabSet = null;
                    string prefabColorID = null;

                    Queue<Element> elementsToCheck = new Queue<Element>();
                    HashSet<ElementId> visitedElements = new HashSet<ElementId>();
                    elementsToCheck.Enqueue(element);
                    visitedElements.Add(element.Id);

                    int depth = 0;
                    while (elementsToCheck.Count > 0 && depth < 3)
                    {
                        int count = elementsToCheck.Count;
                        for (int i = 0; i < count; i++)
                        {
                            Element currentElement = elementsToCheck.Dequeue();
                            foreach (Connector connector in GetConnectors(currentElement))
                            {
                                if (connector.IsConnected)
                                {
                                    Connector connectedConnector = GetConnectedConnector(connector);
                                    if (connectedConnector != null)
                                    {
                                        Element connectedElement = doc.GetElement(connectedConnector.Owner.Id);
                                        if (!visitedElements.Contains(connectedElement.Id))
                                        {
                                            prefabSet = GetParameterValue(connectedElement, "Prefab Set");
                                            prefabColorID = GetParameterValue(connectedElement, "Prefab Color ID");

                                            if (!string.IsNullOrEmpty(prefabSet) && !string.IsNullOrEmpty(prefabColorID))
                                            {
                                                break;
                                            }

                                            elementsToCheck.Enqueue(connectedElement);
                                            visitedElements.Add(connectedElement.Id);
                                        }
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(prefabSet) && !string.IsNullOrEmpty(prefabColorID))
                            {
                                break;
                            }
                        }
                        depth++;
                    }

                    if (string.IsNullOrEmpty(prefabSet) || string.IsNullOrEmpty(prefabColorID))
                    {
                        continue;
                    }

                    // Toewijzen van de Prefab Color ID en Prefab Set parameters
                    Parameter prefabSetParam = element.LookupParameter("Prefab Set");
                    Parameter prefabColorIDParam = element.LookupParameter("Prefab Color ID");

                    if (prefabSetParam != null && prefabColorIDParam != null)
                    {
                        prefabSetParam.Set(prefabSet);
                        prefabColorIDParam.Set(prefabColorID);
                    }
                }

                // Hernummer alle elementen in de prefab set
                RenumberPrefabSets(doc, selectedElements);

                trans.Commit();
            }
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // Selectie geannuleerd door gebruiker: geen fout tonen
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Fout", $"Er is een fout opgetreden tijdens het toewijzen van prefab parameters:\n{ex.Message}");
        }
    }

    // Functie om de parameterwaarde op te halen als string
    private string GetParameterValue(Element element, string paramName)
    {
        Parameter param = element.LookupParameter(paramName);
        if (param != null && param.HasValue)
        {
            return param.AsString();
        }
        return null;
    }

    // Functie om de connectoren van een element op te halen
    private IEnumerable<Connector> GetConnectors(Element element)
    {
        if (element is FamilyInstance familyInstance)
        {
            MEPModel familyMEPModel = familyInstance.MEPModel;
            if (familyMEPModel != null)
            {
                ConnectorSet connectors = familyMEPModel.ConnectorManager.Connectors;
                foreach (Connector connector in connectors)
                {
                    yield return connector;
                }
            }
        }
        else if (element is MEPCurve mepCurve)
        {
            ConnectorSet connectors = mepCurve.ConnectorManager.Connectors;
            foreach (Connector connector in connectors)
            {
                yield return connector;
            }
        }
    }

    // Functie om de verbonden connector op te halen
    private Connector GetConnectedConnector(Connector connector)
    {
        foreach (Connector connected in connector.AllRefs)
        {
            if (connected.Owner.Id != connector.Owner.Id)
            {
                return connected;
            }
        }
        return null;
    }

    // Functie om alle elementen in de prefab set opnieuw te nummeren
    private void RenumberPrefabSets(Document doc, IList<Reference> elements)
    {
        int number = 1;
        foreach (Reference reference in elements)
        {
            Element element = doc.GetElement(reference);
            Parameter prefabNumberParam = element.LookupParameter("Prefab Number");
            if (prefabNumberParam != null)
            {
                prefabNumberParam.Set(number.ToString());
                number++;
            }
        }
    }
}