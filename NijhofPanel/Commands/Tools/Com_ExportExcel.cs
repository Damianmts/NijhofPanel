namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Windows.Forms;
using Views;
using System;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

public class Com_ExportExcel : IExternalEventHandler
{
    public void Execute(UIApplication uiApp)
    {
        var doc = uiApp.ActiveUIDocument.Document;

        // Verzamel alle ViewSchedules uit het document
        var allSchedules = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewSchedule))
            .Cast<ViewSchedule>()
            .Where(vs => !vs.IsTemplate)
            .ToList();

        // Toon het selectie venster
        var selectionWindow = new ScheduleSelectionWindowView(allSchedules);

        if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedSchedules.Any())
        {
            // Vraag gebruiker om map te selecteren
            var folderDialog = new FolderBrowserDialog
            {
                Description = "Selecteer map om Excel bestanden op te slaan"
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var successCount = 0;
                var failCount = 0;
                
                var progressWindow = new ProgressWindowView();
                progressWindow.Show();

                int totalCount = selectionWindow.SelectedSchedules.Count;
                int currentIndex = 0;
                
                
                foreach (var schedule in selectionWindow.SelectedSchedules)
                {
                    currentIndex++;
                    int percentage = (int)((currentIndex / (double)totalCount) * 100);
                    progressWindow.UpdateStatusText($"Exporteren: {schedule.Name} ({currentIndex}/{totalCount})");
                    progressWindow.UpdateProgress(percentage);
                    Application.DoEvents();
                    
                    try
                    {
                        // Maak bestandsnaam
                        var fileName = CleanSheetName(schedule.Name) + ".xlsx";
                        var fullPath = System.IO.Path.Combine(folderDialog.SelectedPath, fileName);

                        using var package = new ExcelPackage();
                        var worksheet = package.Workbook.Worksheets.Add(schedule.Name);

                        // Titel
                        worksheet.Cells[1, 1].Value = schedule.Name;
                        using (var range = worksheet.Cells[1, 1, 1, schedule.GetTableData().GetSectionData(SectionType.Body).NumberOfColumns])
                        {
                            range.Merge = true;
                            range.Style.Font.Bold = true;
                            range.Style.Font.Size = 14;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        // Header
                        var tableData = schedule.GetTableData();
                        var header = tableData.GetSectionData(SectionType.Header);
                        var body = tableData.GetSectionData(SectionType.Body);

                        for (var row = 0; row < header.NumberOfRows; row++)
                        {
                            for (var col = 0; col < header.NumberOfColumns; col++)
                            {
                                worksheet.Cells[row + 2, col + 1].Value =
                                    schedule.GetCellText(SectionType.Header, row, col);
                            }
                        }

                        // Body (snel via 2D-array)
                        int dataRows = body.NumberOfRows;
                        int dataCols = body.NumberOfColumns;

                        // EPPlus 8 verwacht IEnumerable<object[]>
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

                        worksheet.Cells[header.NumberOfRows + 2, 1].LoadFromArrays(rows);
                        
                        if (body.NumberOfRows < 500)
                            worksheet.Cells.AutoFitColumns();

                        package.SaveAs(new System.IO.FileInfo(fullPath));
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        TaskDialog.Show("Fout", $"Fout bij exporteren van '{schedule.Name}': {ex.Message}");
                    }

                }
                
                // Sluit voortgangsvenster
                progressWindow.Close();

                // Forceer garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Toon resultaat
                // TaskDialog.Show("Export Voltooid",
                // $"Export voltooid!\nSuccesvol: {successCount} schedules\nMislukt: {failCount} schedules");
            }
        }
    }

    private string CleanSheetName(string name)
    {
        // Verwijder ongeldige karakters voor Excel sheet namen
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

        // Excel sheet namen mogen maximaal 31 karakters zijn
        if (cleanName.Length > 31)
            cleanName = cleanName.Substring(0, 31);

        return cleanName;
    }

    public string GetName()
    {
        return "Nijhof Panel Export Excel Command";
    }
}