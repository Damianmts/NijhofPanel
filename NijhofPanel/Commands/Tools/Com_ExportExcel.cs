namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using NijhofPanel.Views;
using System;

public class Com_ExportExcel : IExternalEventHandler
{
    public void Execute(UIApplication uiApp)
    {
        Document doc = uiApp.ActiveUIDocument.Document;

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
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecteer map om Excel bestanden op te slaan"
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int successCount = 0;
                int failCount = 0;

                foreach (var schedule in selectionWindow.SelectedSchedules)
                {
                    Excel.Application excelApp = null;
                    Excel.Workbook workbook = null;

                    try
                    {
                        // Maak een schone bestandsnaam
                        string fileName = CleanSheetName(schedule.Name) + ".xlsx";
                        string fullPath = System.IO.Path.Combine(folderDialog.SelectedPath, fileName);

                        excelApp = new Excel.Application();
                        excelApp.Visible = false;
                        workbook = excelApp.Workbooks.Add();

                        // Haal schedule data op
                        var tableData = schedule.GetTableData();
                        var header = tableData.GetSectionData(SectionType.Header);
                        var body = tableData.GetSectionData(SectionType.Body);
                        var worksheet = (Excel.Worksheet)workbook.Worksheets[1];

                        // Titel (rij 1)
                        Excel.Range titleCell = (Excel.Range)worksheet.Cells[1, 1];
                        titleCell.Value = schedule.Name;
                        titleCell.Font.Bold = true;
                        titleCell.Font.Size = 14;
                        Excel.Range titleMergeRange = worksheet.Range[
                            worksheet.Cells[1, 1],
                            worksheet.Cells[1, body.NumberOfColumns]
                        ];
                        titleMergeRange.Merge();
                        titleCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                        // Headers (rijen 2 t/m 7)
                        int headerRowCount = header.NumberOfRows;
                        for (int row = 1; row < headerRowCount; row++)
                        {
                            for (int col = 0; col < header.NumberOfColumns; col++)
                            {
                                string headerText = schedule.GetCellText(SectionType.Header, row, col);
                                Excel.Range headerCell = (Excel.Range)worksheet.Cells[row + 1, col + 1];
                                headerCell.Value = headerText;
                                headerCell.Font.Bold = true;
                                headerCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                            }
                        }

                        // Data (vanaf rij 8)
                        for (int row = 0; row < body.NumberOfRows; row++)
                        {
                            for (int col = 0; col < body.NumberOfColumns; col++)
                            {
                                string cellValue = schedule.GetCellText(SectionType.Body, row, col);
                                Excel.Range cell = (Excel.Range)worksheet.Cells[row + headerRowCount + 2, col + 1]; // +2 voor titel en offset
                                cell.Value = cellValue;
                                cell.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                            }
                        }
                        
                        worksheet.Columns.AutoFit();

                        // Sla het bestand op
                        workbook.SaveAs(fullPath);
                        successCount++;

                        // Expliciet opruimen van worksheet COM object
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        TaskDialog.Show("Fout",
                            $"Fout bij het exporteren van schedule '{schedule.Name}': {ex.Message}");
                    }
                    finally
                    {
                        // Opruimen van COM objecten
                        if (workbook != null)
                        {
                            workbook.Close();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                        }

                        if (excelApp != null)
                        {
                            excelApp.Quit();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                        }
                    }
                }

                // Forceer garbage collection na alle exports
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Toon resultaat
                TaskDialog.Show("Export Voltooid",
                    $"Export voltooid!\nSuccesvol: {successCount} schedules\nMislukt: {failCount} schedules");
            }
        }
    }

    private string CleanSheetName(string name)
    {
        // Verwijder ongeldige karakters voor Excel sheet namen
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        string cleanName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

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