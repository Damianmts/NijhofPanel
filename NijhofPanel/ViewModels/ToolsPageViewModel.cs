namespace NijhofPanel.ViewModels;

using System.Windows.Input;
using Autodesk.Revit.UI;
using Commands.Tools;
using NijhofPanel.Helpers.Core;

public class ToolsPageViewModel : ObservableObject
{
    private readonly ExternalEvent _exportExcelEvent;
    private readonly ExternalEvent _connectElementEvent;
    
    private readonly ExternalEvent _prefabNewEvent;
    private readonly ExternalEvent _prefabAddEvent;
    private readonly ExternalEvent _prefabDeleteEvent;

    // Toolstrip commands
    public ICommand ConnectElementCommand { get; }
    public ICommand ExportExcelCommand { get; }
    
    // Prefab commands
    public ICommand PrefabNewCommand { get; }
    public ICommand PrefabAddCommand { get; }
    public ICommand PrefabDeleteCommand { get; }

    public ToolsPageViewModel()
    {
        var exportExcelHandler = new Com_ExportExcel();
        _exportExcelEvent = ExternalEvent.Create(exportExcelHandler);

        var connectElementHandler = new Com_ConnectElement();
        _connectElementEvent = ExternalEvent.Create(connectElementHandler);

        var prefabNewHandler = new Com_PrefabCreate();
        _prefabNewEvent = ExternalEvent.Create(prefabNewHandler);

        var prefabAddHandler = new Com_PrefabAdd();
        _prefabAddEvent = ExternalEvent.Create(prefabAddHandler);

        var prefabDeleteHandler = new Com_PrefabDelete();
        _prefabDeleteEvent = ExternalEvent.Create(prefabDeleteHandler);

        ConnectElementCommand = new RelayCommands.RelayCommand(ExecuteConnectElement);
        ExportExcelCommand = new RelayCommands.RelayCommand(ExecuteExportExcel);

        PrefabNewCommand = new RelayCommands.RelayCommand(ExecutePrefabCreate);
        PrefabAddCommand = new RelayCommands.RelayCommand(ExecutePrefabAdd);
        PrefabDeleteCommand = new RelayCommands.RelayCommand(ExecutePrefabDelete);
    }

    private void ExecuteConnectElement(object parameter)
    {
        _connectElementEvent.Raise();
    }

    private void ExecuteExportExcel(object parameter)
    {
        _exportExcelEvent.Raise();
    }

    private void ExecutePrefabCreate(object parameter)
    {
        _prefabNewEvent.Raise();
    }

    private void ExecutePrefabAdd(object parameter)
    {
        _prefabAddEvent.Raise();
    }

    private void ExecutePrefabDelete(object parameter)
    {
        _prefabDeleteEvent.Raise();
    }
}