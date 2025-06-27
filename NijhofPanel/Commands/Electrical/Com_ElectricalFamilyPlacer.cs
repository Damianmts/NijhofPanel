namespace NijhofPanel.Commands.Electrical;

using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Helpers;

public class Com_ElectricalFamilyPlacer
{
    private const string BASIS_PAD = @"F:\Stabiplan\Custom\Families\1 - Elektra\BJ Future Wit R24";

    private readonly Dictionary<string, string> _componentMappen = new()
    {
        { "WCD", "WCD" },
        { "ASP", "Aansluitpunten" },
        { "DATA", "Data" },
        { "SCHAK", "Schakelaars" },
        { "VERL", "Verlichting" },
        { "OVG", "Overig" }
    };

    private readonly Dictionary<string, Dictionary<string, string>> _familieMapping =
        new()
        {
            {
                "WCD", new Dictionary<string, string>
                {
                    { "Enkel", "WCD_BJ_Future_1v_Wit.rfa" },
                    { "Dubbel", "WCD_BJ_Future_2v_Wit.rfa" },
                    { "2vOpbouw", "WCD_Gira_2v_Wit.rfa" },
                    { "1vWaterdicht", "WCD_Spatwaterdicht_1v_Wit.rfa" },
                    { "2vWaterdicht", "WCD_Spatwaterdicht_2v_Wit.rfa" },
                    { "Perilex", "WCD_Perilex_ABL_Wit.rfa" },
                    { "Vloer", "WCD_Vloer_1v.rfa" },
                    { "Kracht", "WCD_WP_Krachtstroom.rfa" }
                }
            },
            {
                "ASP", new Dictionary<string, string>
                {
                    { "Bedraad", "Aanpt_Bedraad_BJ_Future_1v_Wit.rfa" },
                    { "Onbedraad", "Aanpt_BJ_Future_1v_Wit.rfa" },
                    { "230v", "Aanpt_230v_In buis_1v.rfa" },
                    { "2x230v", "Aanpt_2x 230v_In buis_1v.rfa" },
                    { "400v", "Aanpt_400v_In buis_1v.rfa" },
                    { "CAP", "Aanpt_CAP_1v_Wit.rfa" }
                }
            },
            {
                "DATA", new Dictionary<string, string>
                {
                    { "Enkel", "Data_Enkel_BJ_Future_1v_Wit.rfa" },
                    { "Dubbel", "Data_Dubbel_BJ_Future_1v_Wit.rfa" },
                    { "Bekabeld", "Data_Bekabeld_1v_Wit.rfa" }
                }
            },
            {
                "SCHAK", new Dictionary<string, string>
                {
                    { "Enkelpolig", "Schak_Enkelp_BJ_Future_1v_Wit.rfa" },
                    { "Dubbelpolig", "Schak_Dubbelp_BJ_Future_1v_Wit.rfa" },
                    { "Vierpolig", "Schak_4polig.rfa" },
                    { "Wissel", "Schak_Wissel_BJ_Future_1v_Wit.rfa" },
                    { "WisselDubbelpolig", "Schak_Wissel Dubbelp_BJ_Future_1v_Wit.rfa" },
                    { "2xWissel", "Schak_Wissel 2x_BJ_Future_1v_Wit.rfa" },
                    { "Serie", "Schak_Serie_BJ_Future_1v_Wit.rfa" },
                    { "Kruis", "Schak_Kruis_BJ_Future_1v_Wit.rfa" },
                    { "Dimmer", "Schak_Leddimmer_BJ_Future_1v_Wit.rfa" },
                    { "DimmerWissel", "Schak_Leddimmer Wissel_BJ_Future_1v_Wit.rfa" },
                    { "Jaloezie", "Schak_Jaloezie_BJ_Future_1v_Wit.rfa" },
                    { "BewegingWand", "Bewegingmelder Wand.rfa" },
                    { "BewegingPlafond", "Bewegingmelder Plafond.rfa" },
                    { "Schemer", "Schemerschakelaar.rfa" }
                }
            },
            {
                "VERL", new Dictionary<string, string>
                {
                    { "Centraaldoos", "Lichtpunt_Centraaldoos.rfa" },
                    { "Plafond", "Lichtpunt_Plafond.rfa" },
                    { "Spot", "Lichtpunt_Inbouwspot.rfa" },
                    { "Wand", "Lichtpunt_Wand.rfa" }
                }
            },
            {
                "OVG", new Dictionary<string, string>
                {
                    { "Rookmelder", "Ovg_Rookmelder.rfa" },
                    { "Bediening", "Ovg_Bediening los.rfa" },
                    { "Bel", "Ovg_Drukknop Bel.rfa" },
                    { "Schel", "Ovg_Schel DingDong.rfa" },
                    { "Intercom", "Ovg_Intercom.rfa" },
                    { "Grondkabel", "Ovg_Grondkabel.rfa" }
                }
            }
        };

    private SymbolPlacementHandler _symbolHandler;
    private ExternalEvent _symbolEvent;

    public (bool success, string message) PlaceElectricalFamily(string componentType, UIDocument uidoc)
    {
        string[] typeDelen = componentType.Split('_');
        if (typeDelen.Length != 2) return (false, "Ongeldig componenttype formaat");

        var hoofdType = typeDelen[0];
        var subType = typeDelen[1];

        if (!ValidateAndGetPaths(hoofdType, subType, out var volledigPad))
            return (false, $"Onbekende component combinatie: {componentType}");

        if (!File.Exists(volledigPad)) return (false, $"Familie niet gevonden: {volledigPad}");

        try
        {
            var doc = uidoc.Document;
            var familyName = Path.GetFileNameWithoutExtension(volledigPad);

            // Eerst controleren of de familie al geladen is
            var symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(e => e.FamilyName.Equals(familyName));

            if (symbol != null)
            {
                // Familie is al geladen, activeer indien nodig
                if (!symbol.IsActive)
                    using (var trans = new Transaction(doc, "Activeer Symbol"))
                    {
                        trans.Start();
                        symbol.Activate();
                        trans.Commit();
                    }
            }
            else
            {
                // Familie moet nog geladen worden
                Family family;
                using (var trans = new Transaction(doc, "Laad Elektrische Familie"))
                {
                    try
                    {
                        trans.Start();
                        if (!doc.LoadFamily(volledigPad, out family)) return (false, "Kon de familie niet laden");

                        // Haal het eerste family symbol op
                        foreach (var symbolId in family.GetFamilySymbolIds())
                        {
                            symbol = doc.GetElement(symbolId) as FamilySymbol;
                            if (symbol != null)
                            {
                                symbol.Activate();
                                break;
                            }
                        }

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        if (trans.GetStatus() == TransactionStatus.Started) trans.RollBack();

                        return (false, $"Fout bij laden familie: {ex.Message}");
                    }
                }
            }

            if (symbol == null) return (false, "Kon geen geldig family symbol vinden");

            // Initialiseer de symbol handler als die nog niet bestaat
            if (_symbolHandler == null)
            {
                _symbolHandler = new SymbolPlacementHandler();
                _symbolEvent = ExternalEvent.Create(_symbolHandler);
            }

            // Stel het symbol in en activeer de externe gebeurtenis
            _symbolHandler.Symbol = symbol;
            _symbolEvent.Raise();

            return (true, "Plaatsingsgereedschap geactiveerd");
        }
        catch (Exception ex)
        {
            return (false, $"Er is een onverwachte fout opgetreden: {ex.Message}");
        }
    }

    private bool ValidateAndGetPaths(string hoofdType, string subType, out string volledigPad)
    {
        volledigPad = string.Empty;

        if (string.IsNullOrEmpty(hoofdType) || string.IsNullOrEmpty(subType) ||
            !_familieMapping.TryGetValue(hoofdType, out var subTypeMapping) ||
            !subTypeMapping.TryGetValue(subType, out var bestandsnaam))
            return false;

        volledigPad = Path.Combine(BASIS_PAD, hoofdType, bestandsnaam);
        Debug.WriteLine($"Opgebouwd pad: {volledigPad}");
        Debug.WriteLine($"Bestand bestaat: {File.Exists(volledigPad)}");
        return true;
    }
}