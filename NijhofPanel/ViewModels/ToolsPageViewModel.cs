namespace NijhofPanel.ViewModels;

using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NijhofPanel.ViewModels;
using NijhofPanel.Helpers;
using NijhofPanel.Commands.Tools;

public class ToolsPageViewModel
{
    private readonly ExternalEvent _externalEvent;
    
    public static ToolsPageViewModel? Instance { get; private set; }
    
    public ICommand ExportExcelCommand { get; }

    public ToolsPageViewModel(ExternalEvent externalEvent)
    {
        _externalEvent = externalEvent;
        _exportExcelHandler = new Com_ExportExcel();
        _externalEvent = ExternalEvent.Create(_exportExcelHandler);
        
        ExportExcelCommand = new RelayCommands.RelayCommand(ExecuteExportExcel);
        
        Instance = this;
    }
    
    private readonly Com_ExportExcel _exportExcelHandler;
    
    private void ExecuteExportExcel(object parameter)
    {
        _externalEvent.Raise();
    }
}