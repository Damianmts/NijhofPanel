namespace NijhofPanel.Commands.Tools;

using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

[Transaction(TransactionMode.Manual)]
public class Com_PlaceFamily
{
    public void Execute(Document doc, string familyPath)
    {
        Console.WriteLine($"[Com_PlaceFamily] Start plaatsactie voor: {familyPath}");

        if (string.IsNullOrEmpty(familyPath) || !File.Exists(familyPath))
        {
            Console.WriteLine("[Com_PlaceFamily] ❌ Ongeldig familypad of bestand bestaat niet.");
            return;
        }

        FamilySymbol? symbol = null;

        // 🔹 1. Probeer eerst te vinden in het document
        Console.WriteLine("[Com_PlaceFamily] 🔍 Zoeken naar bestaand FamilySymbol in het project...");
        symbol = FindExistingSymbol(doc, familyPath);

        if (symbol != null)
            Console.WriteLine($"[Com_PlaceFamily] ✅ Bestaande family gevonden: {symbol.FamilyName} ({symbol.Name})");
        else
            Console.WriteLine("[Com_PlaceFamily] ⚠️ Family nog niet in project — poging tot laden...");

        // 🔹 2. Als niet gevonden → proberen te laden
        if (symbol == null)
        {
            Family? loadedFamily = null;

            using (Transaction t = new Transaction(doc, "Load Family"))
            {
                t.Start();
                bool loaded = doc.LoadFamily(familyPath, out loadedFamily);
                t.Commit();

                Console.WriteLine($"[Com_PlaceFamily] 📦 LoadFamily resultaat: Succes={loaded}, Naam={loadedFamily?.Name ?? "null"}");

                if (!loaded || loadedFamily == null)
                {
                    Console.WriteLine("[Com_PlaceFamily] ❌ LoadFamily is mislukt of gaf null terug.");
                    return;
                }
            }

            // Zoek symbolen in de nieuw geladen family
            var ids = loadedFamily.GetFamilySymbolIds();
            Console.WriteLine($"[Com_PlaceFamily] FamilySymbolIds gevonden: {ids.Count}");

            if (ids.Count > 0)
            {
                symbol = doc.GetElement(ids.First()) as FamilySymbol;
                Console.WriteLine($"[Com_PlaceFamily] ✅ Eerste symbool geselecteerd: {symbol?.Name ?? "null"}");
            }
            else
            {
                Console.WriteLine("[Com_PlaceFamily] ❌ Geen FamilySymbol gevonden in geladen family (mogelijk leeg of systeemtype).");
                return;
            }
        }

        // 🔹 3. Controleer of we nu een symbool hebben
        if (symbol == null)
        {
            Console.WriteLine("[Com_PlaceFamily] ❌ Nog steeds geen symbool na het laden.");
            return;
        }

        // 🔹 4. Activeer (verplicht)
        if (!symbol.IsActive)
        {
            Console.WriteLine($"[Com_PlaceFamily] ⚙️ Symbool '{symbol.Name}' is niet actief — activeren...");
            using (Transaction t = new Transaction(doc, "Activate Family Symbol"))
            {
                t.Start();
                symbol.Activate();
                t.Commit();
            }
            Console.WriteLine("[Com_PlaceFamily] ✅ FamilySymbol succesvol geactiveerd.");
        }

        // 🔹 5. Start plaatsmodus
        try
        {
            UIApplication uiapp = new UIApplication(doc.Application);
            UIDocument uidoc = uiapp.ActiveUIDocument;
            uidoc.PostRequestForElementTypePlacement(symbol);
            Console.WriteLine($"[Com_PlaceFamily] 🚀 Plaatsmodus gestart voor: {symbol.FamilyName} ({symbol.Name})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Com_PlaceFamily] ❌ Fout bij plaatsen van family: {ex.Message}");
        }
    }

    private FamilySymbol? FindExistingSymbol(Document doc, string path)
    {
        string familyName = Path.GetFileNameWithoutExtension(path);
        var symbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .ToList();

        Console.WriteLine($"[Com_PlaceFamily] Project bevat momenteel {symbols.Count} FamilySymbols. Zoekterm: '{familyName}'");

        var found = symbols.FirstOrDefault(f =>
            f.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase) ||
            f.Family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));

        return found;
    }
}