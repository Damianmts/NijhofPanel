namespace NijhofPanel.Commands.Tools;

using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Windows;
using Views;
using System.Linq;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;
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

        // Test of Excel beschikbaar is
        try
        {
            var testExcel = new Excel.Application();
            testExcel.Quit();
            Marshal.ReleaseComObject(testExcel);
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

        int totalCount = _selectedSchedules.Count;
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
        
        foreach (var schedule in _selectedSchedules)
        {
            currentIndex++;
            int percentage = (int)((currentIndex / (double)totalCount) * 100);
            progressWindow.UpdateStatusText($"Exporteren: {schedule.Name} ({currentIndex}/{totalCount})");
            progressWindow.UpdateProgress(percentage);
            System.Windows.Forms.Application.DoEvents();

            Excel.Workbook? workbook = null;

            try
            {
                var fileName = CleanSheetName(schedule.Name) + ".xlsx";
                var fullPath = Path.Combine(basePath, fileName);

                workbook = excelApp.Workbooks.Add();
                
                try
                {
                    excelApp.ScreenUpdating = false;
                    // Calculation soms niet toegestaan in Revit-context → overslaan
                }
                catch { /* negeren */ }

                var worksheet = (Excel.Worksheet)workbook.Worksheets[1];
                var tableData = schedule.GetTableData();
                var header = tableData.GetSectionData(SectionType.Header);
                var body = tableData.GetSectionData(SectionType.Body);

                // Headers (starten direct op de eerste rij)
                var headerRowCount = header.NumberOfRows;
                for (int row = 0; row < headerRowCount; row++)
                {
                    object[,] headerValues = new object[1, header.NumberOfColumns];
                    for (int col = 0; col < header.NumberOfColumns; col++)
                        headerValues[0, col] = schedule.GetCellText(SectionType.Header, row, col);

                    var start = (Excel.Range)worksheet.Cells[row + 1, 1];
                    var end = (Excel.Range)worksheet.Cells[row + 1, header.NumberOfColumns];
                    var headerRange = worksheet.Range[start, end];
                    headerRange.Value2 = headerValues;
                    headerRange.Font.Bold = true;
                    headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                }
                
                int dataRows = body.NumberOfRows;
                int dataCols = body.NumberOfColumns;
                object[,] data = new object[dataRows, dataCols];

                for (int r = 0; r < dataRows; r++)
                {
                    for (int c = 0; c < dataCols; c++)
                        data[r, c] = schedule.GetCellText(SectionType.Body, r, c);
                }

                var startCell = (Excel.Range)worksheet.Cells[headerRowCount + 1, 1];
                var endCell = (Excel.Range)worksheet.Cells[headerRowCount + dataRows, dataCols];
                var writeRange = worksheet.Range[startCell, endCell];
                writeRange.Value2 = data;
                writeRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

                // AutoFit alleen bij kleinere tabellen
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

        progressWindow.Close();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        // TaskDialog.Show("Export voltooid",
        //     $"Excel-export afgerond!\nSuccesvol: {successCount}\nMislukt: {failCount}\n\nBestanden opgeslagen in:\n{basePath}");
    }

    private string CleanSheetName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        if (cleanName.Length > 31)
            cleanName = cleanName.Substring(0, 31);
        return cleanName;
    }

    public string GetName() => "Nijhof Panel Export Excel FittingList";
}