namespace NijhofPanel.Commands.Tools;

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class Com_RecessPlaceFloor : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            var doc = uiDoc.Document;

            using (var tx = new Transaction(doc, "Recess: Place in Floor"))
            {
                tx.Start();

                // TODO: Implementatie voor het plaatsen van een recess in een vloer.
                // - Selecteer Floor host
                // - Maak opening (e.g. Opening or Shaft) of host-based family
                // - Parameters instellen, controle op overlappingen
                // - Validaties en foutafhandeling

                tx.Commit();
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Recess - Floor", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Recess - Place in Floor";
}