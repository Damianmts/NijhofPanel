namespace NijhofPanel.Commands.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Views;
using NijhofPanel.Helpers.Core;

public class Com_ExportCSV : IExternalEventHandler
{
    private readonly IList<ViewSchedule> _selectedSchedules;

    public Com_ExportCSV(IList<ViewSchedule> selectedSchedules)
    {
        _selectedSchedules = selectedSchedules;
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

        // Projectnaam ophalen
        string projectNaam = string.Empty;
        var pNaamParam = projectInfo!.LookupParameter("Projectnaam");
        if (pNaamParam?.HasValue == true)
            projectNaam = pNaamParam.AsString() ?? string.Empty;

        string? input = InputBoxHelper.Show("Voer de projectnaam in:", "Projectnaam invoeren");

        if (string.IsNullOrWhiteSpace(input))
        {
            TaskDialog.Show("Geannuleerd", "Export geannuleerd: projectnaam is vereist voor de zaagmachine-map.");
            return;
        }

        projectNaam = input!;
        
        // Bestandslocatie bepalen
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
        
        // Tweede exportlocatie voor zaagmachine
        var zaagmachineBase = Path.Combine(
            @"T:\Data\!Zaagmachine",
            $"{projectNummer} - {projectNaam}"
        );

        if (!Directory.Exists(zaagmachineBase))
            Directory.CreateDirectory(zaagmachineBase);

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

                    // Data
                    for (int row = 0; row < body.NumberOfRows; row++)
                    {
                        var rowValues = new List<string>();
                        for (int col = 0; col < body.NumberOfColumns; col++)
                        {
                            var cellValue = schedule.GetCellText(SectionType.Body, row, col);
                            rowValues.Add(EscapeCsv(cellValue));
                        }

                        writer.WriteLine(string.Join(";", rowValues));
                    }
                }

                successCount++;
                
                // Kopie naar zaagmachine-map
                try
                {
                    var destPath = Path.Combine(zaagmachineBase, Path.GetFileName(fullPath));
                    File.Copy(fullPath, destPath, true);
                }
                catch (Exception copyEx)
                {
                    TaskDialog.Show("Waarschuwing", $"Kon bestand niet kopiëren naar zaagmachine-map: {copyEx.Message}");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                TaskDialog.Show("Fout", $"Fout bij exporteren van '{schedule.Name}': {ex.Message}");
            }
        }

        progressWindow.Close();
        
        TaskDialog.Show("Export voltooid",
            $"Excel-export afgerond!\nSuccesvol: {successCount}\nMislukt: {failCount}\n\nBestanden opgeslagen in:\n{basePath}");
    }

    public string GetName() => "Nijhof Panel Export CSV Zaaglijst";
    
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