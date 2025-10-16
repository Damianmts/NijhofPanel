namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Views;
using NijhofPanel.Helpers.Core;
using System.Text.RegularExpressions;

public class Com_ExportCSV : IExternalEventHandler
{
    private readonly IList<ViewSchedule> _selectedSchedules;
    private readonly bool _exportToSawMachine;

    public Com_ExportCSV(IList<ViewSchedule> selectedSchedules, bool exportToSawMachine = true)
    {
        _selectedSchedules = selectedSchedules;
        _exportToSawMachine = exportToSawMachine;
    }

    public void Execute(UIApplication uiApp)
    {
        var doc = uiApp.ActiveUIDocument.Document;

        if (_selectedSchedules == null || !_selectedSchedules.Any())
        {
            TaskDialog.Show("Geen selectie", "Er zijn geen zaaglijsten geselecteerd voor export.");
            return;
        }

        // Projectnummer ophalen
        string projectNummer = string.Empty;
        var projectInfo = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectInformation)
            .FirstElement();

        if (projectInfo != null)
        {
            var pNumParam = projectInfo.LookupParameter("Project Nummer");
            if (pNumParam?.HasValue == true)
                projectNummer = pNumParam.AsString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(projectNummer))
        {
            TaskDialog.Show("Fout", "Er kon geen geldig projectnummer worden gevonden in de projectinformatie.");
            return;
        }

        // Projectnaam ophalen (voor later)
        string projectNaam = string.Empty;
        var pNaamParam = projectInfo!.LookupParameter("Projectnaam");
        if (pNaamParam?.HasValue == true)
            projectNaam = pNaamParam.AsString() ?? string.Empty;

        // Zoek in !Zaagmachine naar een map die het projectnummer bevat
        var zaagmachineRoot = @"T:\Data\!Zaagmachine";
        string? bestaandeMap = Directory
            .EnumerateDirectories(zaagmachineRoot)
            .FirstOrDefault(d => Path.GetFileName(d)
                .StartsWith(projectNummer, StringComparison.OrdinalIgnoreCase));

        string zaagmachineBase;

        if (bestaandeMap != null)
        {
            zaagmachineBase = bestaandeMap;
        }
        else
        {
            string? input = InputBoxHelper.Show("Voer de projectnaam in:", "Projectnaam invoeren");

            if (string.IsNullOrWhiteSpace(input))
            {
                TaskDialog.Show("Geannuleerd", "Export geannuleerd: projectnaam is vereist voor de zaagmachine-map.");
                return;
            }

            projectNaam = input!.Trim();
            zaagmachineBase = Path.Combine(zaagmachineRoot, $"{projectNummer} - {projectNaam}");
            Directory.CreateDirectory(zaagmachineBase);
        }

        // Bestandslocatie bepalen (hoofdmappad voor export)
        var invul1 = projectNummer.Length >= 2 ? projectNummer.Substring(0, 2) + "000" : "";
        var invul2 = projectNummer;

        var basePath = Path.Combine(
            @"T:\Data",
            invul1,
            invul2,
            "2.8 Tekeningen",
            "02 Nijhof",
            "03 PDF Prefab tekeningen");

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        int successCount = 0;
        int failCount = 0;

        var progressWindow = new ProgressWindowView();
        progressWindow.Show();

        int totalCount = _selectedSchedules.Count;
        int currentIndex = 0;

        // Exporteren van CSV
        foreach (var schedule in _selectedSchedules)
        {
            currentIndex++;
            int percentage = (int)((currentIndex / (double)totalCount) * 100);
            progressWindow.UpdateStatusText($"Exporteren: {schedule.Name} ({currentIndex}/{totalCount})");
            progressWindow.UpdateProgress(percentage);
            System.Windows.Forms.Application.DoEvents();

            try
            {
                var tableData = schedule.GetTableData();
                var header = tableData.GetSectionData(SectionType.Header);
                var body = tableData.GetSectionData(SectionType.Body);

                var fileName = CleanFileName(schedule.Name) + ".csv";
                var fullPath = Path.Combine(basePath, fileName);
                string newFullPath = fullPath;
                string summarySuffix = ""; // buiten de using gedefinieerd

                using (var writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
                {
                    // Titel
                    writer.WriteLine(schedule.Name);
                    writer.WriteLine();

                    // Headers
                    for (int row = 1; row < header.NumberOfRows; row++)
                    {
                        var headerValues = new List<string>();
                        for (int col = 0; col < header.NumberOfColumns; col++)
                        {
                            var headerText = schedule.GetCellText(SectionType.Header, row, col);
                            headerValues.Add(EscapeCsv(headerText));
                        }

                        writer.WriteLine(string.Join(";", headerValues));
                    }

                    // Data (met uitsluiting van 5m buizen/ducts)
                    var excludedItems = new Dictionary<string, int>();

                    for (int row = 0; row < body.NumberOfRows; row++)
                    {
                        var rowValues = new List<string>();
                        for (int col = 0; col < body.NumberOfColumns; col++)
                        {
                            var cellValue = schedule.GetCellText(SectionType.Body, row, col);
                            rowValues.Add(cellValue);
                        }

                        bool isFiveMeter = rowValues.Any(v =>
                            v.Trim().Equals("5000", StringComparison.OrdinalIgnoreCase) ||
                            v.Trim().Equals("5000.0", StringComparison.OrdinalIgnoreCase) ||
                            v.Trim().Equals("5.000", StringComparison.OrdinalIgnoreCase)
                        );

                        if (isFiveMeter)
                        {
                            string raw = string.Join(" ", rowValues.Select(x => x?.ToString() ?? ""));
                            string key = "5m onbekend";

                            // Buisdetectie (zoek naar diameter in mm)
                            var match = Regex.Match(raw, @"[Øø]?\s*(\d+(\.\d+)?)\s*mm",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                // Neem alleen het numerieke deel voor de diameter
                                string diameter = match.Groups[1].Value;
                                if (diameter.EndsWith(".0"))
                                    diameter = diameter.Substring(0, diameter.Length - 2);
                                key = $"5m ø{diameter} mm";
                            }
                            // Ductdetectie (195/80 → 195-80)
                            else if (raw.Contains("/"))
                            {
                                var maat = raw.Split(' ').FirstOrDefault(v => v.Contains("/"));
                                if (!string.IsNullOrEmpty(maat))
                                {
                                    maat = maat.Replace("/", "-");
                                    key = $"{maat} 5m";
                                }
                            }

                            if (!excludedItems.ContainsKey(key))
                                excludedItems[key] = 0;
                            excludedItems[key]++;
                            continue;
                        }

                        var escaped = rowValues.Select(EscapeCsv);
                        writer.WriteLine(string.Join(";", escaped));
                    }

                    // Alleen toewijzing van summarySuffix
                    if (excludedItems.Any())
                    {
                        var summaryParts = excludedItems.Select(kvp => $"+ {kvp.Value}x {kvp.Key}");
                        summarySuffix = " (" + string.Join(" ", summaryParts) + ")";
                    }
                } // einde using: writer gesloten

                // Bestand hernoemen na sluiting
                newFullPath = Path.Combine(
                    basePath,
                    Path.GetFileNameWithoutExtension(fullPath) + summarySuffix + ".csv"
                );

                if (!string.Equals(newFullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(newFullPath))
                        File.Delete(newFullPath);
                    File.Move(fullPath, newFullPath);
                }
                else
                {
                    newFullPath = fullPath;
                }

                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                TaskDialog.Show("Fout", $"Fout bij exporteren van '{schedule.Name}': {ex.Message}");
            }
        }
        
        // Batch-kopie naar zaagmachine-map (na export)
        if (_exportToSawMachine)
        {
            try
            {
                progressWindow.UpdateStatusText("Bestanden kopiëren naar zaagmachine-map...");
                progressWindow.UpdateProgress(100);
                System.Windows.Forms.Application.DoEvents();

                foreach (var file in Directory.GetFiles(basePath, "*.csv"))
                {
                    var destPath = Path.Combine(zaagmachineBase, Path.GetFileName(file));
                    File.Copy(file, destPath, true);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Waarschuwing",
                    $"Kopiëren naar zaagmachine-map mislukt: {ex.Message}");
            }
        }
        
        progressWindow.Close();

        var td = new TaskDialog("Export voltooid")
        {
            MainInstruction = "CSV-export afgerond!",
            MainContent =
                $"Succesvol: {successCount}\nMislukt: {failCount}\n\nBestanden opgeslagen in:\n{basePath}"
                + (_exportToSawMachine
                    ? $"\n\nZaagmachine-map:\n{zaagmachineBase}"
                    : string.Empty),
            CommonButtons = TaskDialogCommonButtons.Close
        };

        // Alleen de juiste knoppen tonen
        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "📂 Open Exportmap");

        if (_exportToSawMachine)
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "⚙️ Open Zaagmachine-map");

        bool stayOpen = true;
        while (stayOpen)
        {
            var result = td.Show();

            try
            {
                if (result == TaskDialogResult.CommandLink1)
                {
                    System.Diagnostics.Process.Start("explorer.exe", basePath);
                }
                else if (_exportToSawMachine && result == TaskDialogResult.CommandLink2)
                {
                    System.Diagnostics.Process.Start("explorer.exe", zaagmachineBase);
                }
                else
                {
                    stayOpen = false; // gebruiker sluit venster
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fout", $"Kon de map niet openen: {ex.Message}");
            }
        }
    }

    public string GetName() => "Nijhof Panel Export CSV SawList";

    private string CleanFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return cleanName.Length > 50 ? cleanName.Substring(0, 50) : cleanName;
    }

    private string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            value = "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
