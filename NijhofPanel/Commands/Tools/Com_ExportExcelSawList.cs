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

        string basePath;

        var invul1 = projectNummer.Length >= 2 ? projectNummer.Substring(0, 2) + "000" : "";
        var invul2 = projectNummer;

        basePath = Path.Combine(
            @"T:\Data",
            invul1,
            invul2,
            "2.8 Tekeningen",
            "02 Nijhof",
            "03 PDF Prefab tekeningen");

        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

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
        
        foreach (var schedule in _selectedSchedules)
        {
            currentIndex++;
            int percentage = (int)((currentIndex / (double)totalCount) * 100);
            progressWindow.UpdateStatusText($"Exporteren: {schedule.Name} ({currentIndex}/{totalCount})");
            progressWindow.UpdateProgress(percentage);
            System.Windows.Forms.Application.DoEvents();
            
            Excel.Application? excelApp = null;
            Excel.Workbook? workbook = null;

            try
            {
                var fileName = CleanSheetName(schedule.Name) + ".xlsx";
                var fullPath = Path.Combine(basePath, fileName);

                excelApp = new Excel.Application();
                excelApp.Visible = false;
                workbook = excelApp.Workbooks.Add();

                var tableData = schedule.GetTableData();
                var header = tableData.GetSectionData(SectionType.Header);
                var body = tableData.GetSectionData(SectionType.Body);
                var worksheet = (Excel.Worksheet)workbook.Worksheets[1];

                // Titel
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

                // Headers
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

                // Data
                for (var row = 0; row < body.NumberOfRows; row++)
                for (var col = 0; col < body.NumberOfColumns; col++)
                {
                    var cellValue = schedule.GetCellText(SectionType.Body, row, col);
                    var cell = (Excel.Range)worksheet.Cells[row + headerRowCount + 2, col + 1];
                    cell.Value = cellValue;
                    cell.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                }

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

    public string GetName() => "Nijhof Panel Export Excel SawList";
}