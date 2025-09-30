namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Views;
using System;
using Excel = Microsoft.Office.Interop.Excel;

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
                
                Excel.Application? testExcelApp;
                try
                {
                    testExcelApp = new Excel.Application();
                    testExcelApp.Quit();
                    Marshal.ReleaseComObject(testExcelApp);
                }
                catch (COMException)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Excel fout",
                        "Excel kan niet gestart worden.\n\n" +
                        "Mogelijke oorzaak: verschil tussen 32- en 64-bit versies van Revit en Microsoft Office.\n" +
                        "Oplossing: installeer de 64-bit versie van Office of gebruik een andere exportmethode.");
                    return; // Stop de hele export
                }

                foreach (var schedule in selectionWindow.SelectedSchedules)
                {
                    Excel.Application? excelApp = null;
                    Excel.Workbook? workbook = null;

                    try
                    {
                        // Maak een schone bestandsnaam
                        var fileName = CleanSheetName(schedule.Name) + ".xlsx";
                        var fullPath = System.IO.Path.Combine(folderDialog.SelectedPath, fileName);

                        excelApp = new Excel.Application();
                        excelApp.Visible = false;
                        workbook = excelApp.Workbooks.Add();

                        // Haal schedule data op
                        var tableData = schedule.GetTableData();
                        var header = tableData.GetSectionData(SectionType.Header);
                        var body = tableData.GetSectionData(SectionType.Body);
                        var worksheet = (Excel.Worksheet)workbook.Worksheets[1];

                        // Titel (rij 1)
                        var titleCell = (Excel.Range)worksheet.Cells[1, 1];
                        titleCell.Value = schedule.Name;
                        titleCell.Font.Bold = true;
                        titleCell.Font.Size = 14;
                        var titleMergeRange = worksheet.Range[
                            worksheet.Cells[1, 1],
                            worksheet.Cells[1, body.NumberOfColumns]
                        ];
                        titleMergeRange.Merge();
                        titleCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                        // Headers (rijen 2 t/m 7)
                        var headerRowCount = header.NumberOfRows;
                        for (var row = 1; row < headerRowCount; row++)
                        for (var col = 0; col < header.NumberOfColumns; col++)
                        {
                            var headerText = schedule.GetCellText(SectionType.Header, row, col);
                            var headerCell = (Excel.Range)worksheet.Cells[row + 1, col + 1];
                            headerCell.Value = headerText;
                            headerCell.Font.Bold = true;
                            headerCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        }

                        // Data (vanaf rij 8)
                        for (var row = 0; row < body.NumberOfRows; row++)
                        for (var col = 0; col < body.NumberOfColumns; col++)
                        {
                            var cellValue = schedule.GetCellText(SectionType.Body, row, col);
                            var cell = (Excel.Range)worksheet.Cells[row + headerRowCount + 2,
                                col + 1]; // +2 voor titel en offset
                            cell.Value = cellValue;
                            cell.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                        }

                        worksheet.Columns.AutoFit();

                        // Sla het bestand op
                        workbook.SaveAs(fullPath);
                        successCount++;

                        // Expliciet opruimen van worksheet COM object
                        Marshal.ReleaseComObject(worksheet);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Autodesk.Revit.UI.TaskDialog.Show("Fout",
                            $"Fout bij het exporteren van schedule '{schedule.Name}': {ex.Message}");
                    }
                    finally
                    {
                        // Opruimen van COM objecten
                        if (workbook != null)
                        {
                            workbook.Close();
                            Marshal.ReleaseComObject(workbook);
                        }

                        if (excelApp != null)
                        {
                            excelApp.Quit();
                            Marshal.ReleaseComObject(excelApp);
                        }
                    }
                }

                // Forceer garbage collection na alle exports
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Toon resultaat
                Autodesk.Revit.UI.TaskDialog.Show("Export Voltooid",
                    $"Export voltooid!\nSuccesvol: {successCount} schedules\nMislukt: {failCount} schedules");
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