namespace NijhofPanel.ViewModels;

using System.Windows.Input;
using Autodesk.Revit.UI;
using Commands.Tools;
using NijhofPanel.Helpers.Core;

public class ToolsPageViewModel
{
    private readonly ExternalEvent _exportExcelEvent;
    private readonly ExternalEvent _connectElementEvent;

    public static ToolsPageViewModel? Instance { get; private set; }

    public ICommand ConnectElementCommand { get; }
    public ICommand ExportExcelCommand { get; }

    public ToolsPageViewModel()
    {
        var exportExcelHandler = new Com_ExportExcel();
        _exportExcelEvent = ExternalEvent.Create(exportExcelHandler);

        var connectElementHandler = new Com_ConnectElement();
        _connectElementEvent = ExternalEvent.Create(connectElementHandler);

        ConnectElementCommand = new RelayCommands.RelayCommand(ExecuteConnectElement);
        ExportExcelCommand = new RelayCommands.RelayCommand(ExecuteExportExcel);

        Instance = this;
    }

    private void ExecuteConnectElement(object parameter)
    {
        _connectElementEvent.Raise();
    }

    private void ExecuteExportExcel(object parameter)
    {
        _exportExcelEvent.Raise();
    }
}