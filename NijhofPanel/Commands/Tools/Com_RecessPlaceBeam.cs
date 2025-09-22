namespace NijhofPanel.Commands.Tools;

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class Com_RecessPlaceBeam : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            var doc = uiDoc.Document;

            using (var tx = new Transaction(doc, "Recess: Place in Beam"))
            {
                tx.Start();

                // TODO: Implementatie voor het plaatsen van een recess in een ligger/beam.
                // - Selecteer host (FamilyInstance / Framing)
                // - Bepaal positie en afmetingen
                // - Plaats opening of host-based family
                // - Validaties en foutafhandeling

                tx.Commit();
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Recess - Beam", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Recess - Place in Beam";
}