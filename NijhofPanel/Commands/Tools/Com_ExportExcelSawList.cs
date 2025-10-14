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
using System.Text.RegularExpressions;
using Views;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

public class Com_ExportExcelSawList : IExternalEventHandler
{
    private readonly IList<ViewSchedule> _selectedSchedules;

    public Com_ExportExcelSawList(IList<ViewSchedule> selectedSchedules)
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

        // Map bepalen
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
                var fileName = CleanSheetName(schedule.Name) + ".xlsx";
                var fullPath = Path.Combine(basePath, fileName);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(schedule.Name);

                var tableData = schedule.GetTableData();
                var header = tableData.GetSectionData(SectionType.Header);
                var body = tableData.GetSectionData(SectionType.Body);

                // ===== Titel =====
                worksheet.Cells[1, 1].Value = schedule.Name;
                using (var titleRange = worksheet.Cells[1, 1, 1, body.NumberOfColumns])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.Bold = true;
                    titleRange.Style.Font.Size = 14;
                    titleRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // ===== Headers =====
                var headerRowCount = header.NumberOfRows;

                // Controleer of eerste headerregel gelijk is aan de titel → dan overslaan
                int headerStartIndex = 0;
                if (headerRowCount > 0)
                {
                    string firstHeaderText = schedule.GetCellText(SectionType.Header, 0, 0)?.Trim() ?? "";
                    if (string.Equals(firstHeaderText, schedule.Name, StringComparison.OrdinalIgnoreCase))
                        headerStartIndex = 1;
                }

                // Schrijf alleen de headerregels die niet de titel dupliceren
                for (int row = headerStartIndex; row < headerRowCount; row++)
                {
                    for (int col = 0; col < header.NumberOfColumns; col++)
                    {
                        worksheet.Cells[(row - headerStartIndex) + 2, col + 1].Value =
                            schedule.GetCellText(SectionType.Header, row, col);
                    }
                }

                // Headerstijl – exact over de header, zonder lege rij
                int styledHeaderRows = headerRowCount - headerStartIndex;
                
                // Bepaal de laatst gebruikte kolom in de header (zodat de grijze stijl niet te ver doorloopt)
                int lastUsedHeaderCol = header.NumberOfColumns;
                for (int c = header.NumberOfColumns - 1; c >= 0; c--)
                {
                    bool colIsEmpty = true;
                    for (int r = headerStartIndex; r < headerRowCount; r++)
                    {
                        var text = schedule.GetCellText(SectionType.Header, r, c)?.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            colIsEmpty = false;
                            break;
                        }
                    }
                    if (!colIsEmpty)
                    {
                        lastUsedHeaderCol = c + 1; // 1-based kolomindex
                        break;
                    }
                }

                // Headerstijl – exact over de gebruikte headerkolommen
                using (var headerRange = worksheet.Cells[2, 1, styledHeaderRows + 1, lastUsedHeaderCol])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // ===== Lege rij onder header =====
                int dataStartRow = styledHeaderRows + 3; // +1 voor titel, +header, +1 lege rij

                // ===== Data (body) met uitsluiting van 5m buizen/ducts =====
                int dataRows = body.NumberOfRows;
                int dataCols = body.NumberOfColumns;

                var rows = new List<object[]>(); // Alleen regels die niet 5m zijn
                var excludedItems = new Dictionary<string, int>(); // Houdt bij wat we overslaan

                for (int r = 0; r < dataRows; r++)
                {
                    var rowValues = new object[dataCols];
                    for (int c = 0; c < dataCols; c++)
                        rowValues[c] = schedule.GetCellText(SectionType.Body, r, c);

                    // Controleer of dit een 5m-element is
                    bool isFiveMeter = rowValues.Any(v =>
                    {
                        var text = v?.ToString()?.Trim() ?? "";
                        return text.Equals("5000", StringComparison.OrdinalIgnoreCase)
                               || text.Equals("5000.0", StringComparison.OrdinalIgnoreCase)
                               || text.Equals("5.000", StringComparison.OrdinalIgnoreCase);
                    });

                    if (isFiveMeter)
                    {
                        string raw = string.Join(" ", rowValues.Select(x => x?.ToString() ?? ""));
                        string key = "5m onbekend";

                        // === Buisdetectie (zoek naar diameter in mm) ===
                        var match = Regex.Match(raw, @"[Øø]?\s*(\d+(\.\d+)?)\s*mm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            // Neem alleen het numerieke deel voor de diameter
                            string diameter = match.Groups[1].Value;
                            if (diameter.EndsWith(".0"))
                                diameter = diameter.Substring(0, diameter.Length - 2);
                            key = $"5m ø{diameter} mm";
                        }
                        // === Ductdetectie (195/80 → 195-80) ===
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
                        continue; // niet toevoegen aan Excel-lijst
                    }

                    rows.Add(rowValues);
                }

                // ===== Plaats data =====
                worksheet.Cells[dataStartRow, 1].LoadFromArrays(rows);

                // ===== Layout =====
                if (body.NumberOfRows < 500)
                    worksheet.Cells.AutoFitColumns();

                // Border tot en met laatste datarij (één rij minder)
                int lastRow = dataStartRow + rows.Count - 1;
                using (var range = worksheet.Cells[1, 1, lastRow, dataCols])
                {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // ===== Bestandsnaam met samenvatting =====
                string summarySuffix = "";
                if (excludedItems.Any())
                {
                    var summaryParts = excludedItems.Select(kvp => $"+ {kvp.Value}x {kvp.Key}");
                    summarySuffix = " (" + string.Join(" ", summaryParts) + ")";
                }

                var newFileName = Path.GetFileNameWithoutExtension(fullPath) + summarySuffix + ".xlsx";
                var newFullPath = Path.Combine(basePath, newFileName);

                if (File.Exists(newFullPath))
                    File.Delete(newFullPath);

                package.SaveAs(new FileInfo(newFullPath));
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

    private string CleanSheetName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        if (cleanName.Length > 31)
            cleanName = cleanName.Substring(0, 31);
        return cleanName;
    }

    public string GetName() => "Nijhof Panel Export Excel SawList";
}