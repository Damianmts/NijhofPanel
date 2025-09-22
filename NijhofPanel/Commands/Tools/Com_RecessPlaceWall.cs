namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

public class Com_RecessPlaceWall : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            var doc = uiDoc.Document;

            using (var tx = new Transaction(doc, "Recess: Place in Wall"))
            {
                tx.Start();

                // TODO: Implementatie voor het plaatsen van een recess in een vloer.
                // - Selecteer Wall host
                // - Maak opening (e.g. Opening or Shaft) of host-based family
                // - Parameters instellen, controle op overlappingen
                // - Validaties en foutafhandeling

                tx.Commit();
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Recess - Wall", $"Er is een fout opgetreden:\n{ex.Message}");
        }
    }

    public string GetName() => "Recess - Place in Wall";
}