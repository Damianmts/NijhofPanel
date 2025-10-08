namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Views;
using System;
using Excel = Microsoft.Office.Interop.Excel;
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

                // Test of Excel beschikbaar is
                Excel.Application? testExcelApp;
                try
                {
                    testExcelApp = new Excel.Application();
                    testExcelApp.Quit();
                    Marshal.ReleaseComObject(testExcelApp);
                }
                catch (COMException)
                {
                    TaskDialog.Show("Excel fout",
                        "Excel kan niet gestart worden.\n\n" +
                        "Mogelijke oorzaak: verschil tussen 32- en 64-bit versies van Revit en Microsoft Office.\n" +
                        "Oplossing: installeer de 64-bit versie van Office of gebruik een andere exportmethode.");
                    return;
                }
                
                var progressWindow = new ProgressWindowView();
                progressWindow.Show();

                int totalCount = selectionWindow.SelectedSchedules.Count;
                int currentIndex = 0;
                
                var excelApp = new Excel.Application
                {
                    Visible = false,
                    DisplayAlerts = false
                };
        
                try
                {
                    excelApp.ScreenUpdating = false;
                    // Calculation-property soms niet beschikbaar → overslaan
                }
                catch { /* negeren */ }

                foreach (var schedule in selectionWindow.SelectedSchedules)
                {
                    currentIndex++;
                    int percentage = (int)((currentIndex / (double)totalCount) * 100);
                    progressWindow.UpdateStatusText($"Exporteren: {schedule.Name} ({currentIndex}/{totalCount})");
                    progressWindow.UpdateProgress(percentage);
                    Application.DoEvents();

                    Excel.Workbook? workbook = null;

                    try
                    {
                        // Maak bestandsnaam
                        var fileName = CleanSheetName(schedule.Name) + ".xlsx";
                        var fullPath = System.IO.Path.Combine(folderDialog.SelectedPath, fileName);

                        workbook = excelApp.Workbooks.Add();
                
                        try
                        {
                            excelApp.ScreenUpdating = false;
                            // Calculation-property soms niet beschikbaar → overslaan
                        }
                        catch { /* negeren */ }

                        var tableData = schedule.GetTableData();
                        var header = tableData.GetSectionData(SectionType.Header);
                        var body = tableData.GetSectionData(SectionType.Body);
                        var worksheet = (Excel.Worksheet)workbook.Worksheets[1];

                        // Titel (rij 1)
                        var titleCell = (Excel.Range)worksheet.Cells[1, 1];
                        titleCell.Value2 = schedule.Name;
                        titleCell.Font.Bold = true;
                        titleCell.Font.Size = 14;
                        var titleMergeRange = worksheet.Range[
                            worksheet.Cells[1, 1],
                            worksheet.Cells[1, body.NumberOfColumns]
                        ];
                        titleMergeRange.Merge();
                        titleCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                        // Headers
                        var headerRowCount = header.NumberOfRows;
                        for (var row = 1; row < headerRowCount; row++)
                        {
                            object[,] headerValues = new object[1, header.NumberOfColumns];
                            for (var col = 0; col < header.NumberOfColumns; col++)
                            {
                                headerValues[0, col] = schedule.GetCellText(SectionType.Header, row, col);
                            }

                            var headerStart = (Excel.Range)worksheet.Cells[row + 1, 1];
                            var headerEnd = (Excel.Range)worksheet.Cells[row + 1, header.NumberOfColumns];
                            var headerRange = worksheet.Range[headerStart, headerEnd];
                            headerRange.Value2 = headerValues;
                            headerRange.Font.Bold = true;
                            headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        }

                        // ✅ Data via 2D-array (veel sneller)
                        int dataRows = body.NumberOfRows;
                        int dataCols = body.NumberOfColumns;
                        object[,] data = new object[dataRows, dataCols];

                        for (int r = 0; r < dataRows; r++)
                        {
                            for (int c = 0; c < dataCols; c++)
                            {
                                data[r, c] = schedule.GetCellText(SectionType.Body, r, c);
                            }
                        }

                        var startCell = (Excel.Range)worksheet.Cells[headerRowCount + 2, 1];
                        var endCell = (Excel.Range)worksheet.Cells[headerRowCount + 1 + dataRows, dataCols];
                        var writeRange = worksheet.Range[startCell, endCell];
                        writeRange.Value2 = data;
                        writeRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

                        // Kolombreedtes automatisch aanpassen alleen bij kleinere tabellen
                        if (body.NumberOfRows < 500)
                            worksheet.Columns.AutoFit();

                        workbook.SaveAs(fullPath);
                        successCount++;

                        Marshal.ReleaseComObject(worksheet);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        TaskDialog.Show("Fout",
                            $"Fout bij exporteren van '{schedule.Name}': {ex.Message}");
                    }
                    finally
                    {
                        if (workbook != null)
                        {
                            workbook.Close(false);
                            Marshal.ReleaseComObject(workbook);
                        }
                    }
                }
                
                excelApp.ScreenUpdating = true;
                excelApp.Quit();
                Marshal.ReleaseComObject(excelApp);

                // ✅ Sluit voortgangsvenster
                progressWindow.Close();

                // Forceer garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Toon resultaat
                // TaskDialog.Show("Export Voltooid",
                //     $"Export voltooid!\nSuccesvol: {successCount} schedules\nMislukt: {failCount} schedules");
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