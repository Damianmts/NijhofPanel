namespace NijhofPanel.ViewModels;

using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ViewModels;
using Helpers;
using Commands.Tools;

public class ToolsPageViewModel
{
    private readonly ExternalEvent _exportExcelEvent;
    private readonly ExternalEvent _connectElementEvent;

    public static ToolsPageViewModel? Instance { get; private set; }

    public ICommand ConnectElementCommand { get; }
    public ICommand ExportExcelCommand { get; }

    public ToolsPageViewModel(ExternalEvent externalEvent)
    {
        _exportExcelHandler = new Com_ExportExcel();
        _exportExcelEvent = ExternalEvent.Create(_exportExcelHandler);

        _connectElementHandler = new Com_ConnectElement();
        _connectElementEvent = ExternalEvent.Create(_connectElementHandler);

        ConnectElementCommand = new RelayCommands.RelayCommand(ExecuteConnectElement);
        ExportExcelCommand = new RelayCommands.RelayCommand(ExecuteExportExcel);

        Instance = this;
    }

    private readonly Com_ConnectElement _connectElementHandler;

    private void ExecuteConnectElement(object parameter)
    {
        _connectElementEvent.Raise();
    }

    private readonly Com_ExportExcel _exportExcelHandler;

    private void ExecuteExportExcel(object parameter)
    {
        _exportExcelEvent.Raise();
    }
}