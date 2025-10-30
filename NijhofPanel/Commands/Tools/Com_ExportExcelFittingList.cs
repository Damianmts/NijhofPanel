namespace NijhofPanel.Commands.Tools;

using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Views;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

public class Com_ExportExcelFittingList : IExternalEventHandler
{
    private readonly IList<ViewSchedule> _selectedSchedules;

    public Com_ExportExcelFittingList(IList<ViewSchedule> selectedSchedules)
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

        // Pad bepalen
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

        foreach (var schedule in _selectedSchedules)
        {
            currentIndex++;
            int percentage = (int)((currentIndex / (double)totalCount) * 100);
            progressWindow.UpdateStatusText($"Exporteren: {schedule.Name} ({currentIndex}/{totalCount})");
            progressWindow.UpdateProgress(percentage);
            Application.DoEvents();

            try
            {
                // Bestandsnaam met projectnummer
                var fileName = $"{projectNummer} - {CleanFileName(schedule.Name)}.xlsx";
                var fullPath = Path.Combine(basePath, fileName);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Blad1");

                var tableData = schedule.GetTableData();
                var header = tableData.GetSectionData(SectionType.Header);
                var body = tableData.GetSectionData(SectionType.Body);

                // ===== Headers (titelregel uitsluiten als die gelijk is aan schedule-naam) =====
                int headerRowCount = header.NumberOfRows;
                int headerColCount = header.NumberOfColumns;

                // Controleer of de eerste headercel gelijk is aan de titel → dan overslaan
                int headerStartIndex = 0;
                if (headerRowCount > 0)
                {
                    string firstHeaderText = schedule.GetCellText(SectionType.Header, 0, 0)?.Trim() ?? "";
                    if (string.Equals(firstHeaderText, schedule.Name, StringComparison.OrdinalIgnoreCase))
                        headerStartIndex = 1;
                }

                // Headers schrijven (vanaf headerStartIndex)
                for (int r = headerStartIndex; r < headerRowCount; r++)
                {
                    for (int c = 0; c < headerColCount; c++)
                    {
                        worksheet.Cells[(r - headerStartIndex) + 1, c + 1].Value =
                            schedule.GetCellText(SectionType.Header, r, c);
                    }
                }

                // Header-opmaak
                int styledHeaderRows = headerRowCount - headerStartIndex;
                if (styledHeaderRows > 0)
                {
                    using (var headerRange = worksheet.Cells[1, 1, styledHeaderRows, headerColCount])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }
                }

                // Body data → IEnumerable<object[]>
                int dataRows = body.NumberOfRows;
                int dataCols = body.NumberOfColumns;
                var rows = new List<object[]>(dataRows);

                for (int r = 0; r < dataRows; r++)
                {
                    var rowValues = new object[dataCols];
                    for (int c = 0; c < dataCols; c++)
                    {
                        rowValues[c] = schedule.GetCellText(SectionType.Body, r, c);
                    }
                    rows.Add(rowValues);
                }

                // Body onder header zetten
                worksheet.Cells[styledHeaderRows + 1, 1].LoadFromArrays(rows);

                if (body.NumberOfRows < 500)
                    worksheet.Cells.AutoFitColumns();

                // Rand toevoegen
                using (var range = worksheet.Cells[1, 1, styledHeaderRows + dataRows, dataCols])
                {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                package.SaveAs(new FileInfo(fullPath));
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                TaskDialog.Show("Fout", $"Fout bij exporteren van '{schedule.Name}': {ex.Message}");
            }
        }

        progressWindow.Close();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Dialoog met één extra knop om de map te openen
        var td = new TaskDialog("Export voltooid")
        {
            MainInstruction = "Excel-export afgerond!",
            MainContent =
                $"Succesvol: {successCount}\nMislukt: {failCount}\n\nBestanden opgeslagen in:\n{basePath}",
            CommonButtons = TaskDialogCommonButtons.Close
        };

        // Knop toevoegen om de map te openen
        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "📂 Open Exportmap");

        var result = td.Show();

        // Als gebruiker op de knop drukt → map openen
        if (result == TaskDialogResult.CommandLink1)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", basePath);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fout", $"Kon de map niet openen: {ex.Message}");
            }
        }
    }

    private string CleanFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Onbekend";

        // Verwijder ongeldige tekens voor Windows-bestanden
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

        return cleanName.Trim();
    }

    public string GetName() => "Nijhof Panel Export Excel FittingList";
}