namespace NijhofPanel.ViewModels;

using System.Windows.Input;
using Autodesk.Revit.UI;
using Commands.Tools;
using NijhofPanel.Helpers.Core;

public class ToolsPageViewModel : ObservableObject
{
    private readonly ExternalEvent _exportExcelEvent;
    private readonly ExternalEvent _connectElementEvent;
    private readonly ExternalEvent _updateHWAEvent;
    private readonly ExternalEvent _updateHWALengthEvent;
    private readonly ExternalEvent _splitPipeEvent;
    
    private readonly ExternalEvent _recessPlaceWallEvent;
    private readonly ExternalEvent _recessPlaceBeamEvent;
    private readonly ExternalEvent _recessPlaceFloorEvent;
    
    private readonly ExternalEvent _prefabNewEvent;
    private readonly ExternalEvent _prefabAddEvent;
    private readonly ExternalEvent _prefabDeleteEvent;
    
    private readonly ExternalEvent _tagMVLengthEvent;
    private readonly ExternalEvent _tagHWALengthEvent;
    private readonly ExternalEvent _tagVWALengthEvent;
    private readonly ExternalEvent _create3DViewEvent;
    private readonly ExternalEvent _refreshViewEvent;

    // Toolstrip commands
    public ICommand ConnectElementCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand UpdateHWACommand { get; }
    public ICommand UpdateHWALengthCommand { get; }
    public ICommand SplitPipeCommand { get; }
    
    // Recess commands
    public ICommand RecessPlaceWallCommand { get; }
    public ICommand RecessPlaceBeamCommand { get; }
    public ICommand RecessPlaceFloorCommand { get; }
    
    // Prefab commands
    public ICommand PrefabNewCommand { get; }
    public ICommand PrefabAddCommand { get; }
    public ICommand PrefabDeleteCommand { get; }
    
    // Tag commands
    public ICommand TagMVLengthCommand { get; }
    public ICommand TagHWALengthCommand { get; }
    public ICommand TagVWALengthCommand { get; }
    public ICommand Create3DViewCommand { get; }
    public ICommand RefreshViewCommand { get; }

    public ToolsPageViewModel()
    {
        var exportExcelHandler = new Com_ExportExcel();
        _exportExcelEvent = ExternalEvent.Create(exportExcelHandler);

        var connectElementHandler = new Com_ConnectElement();
        _connectElementEvent = ExternalEvent.Create(connectElementHandler);

        var updateHWAHandler = new Com_UpdateHWA();
        _updateHWAEvent = ExternalEvent.Create(updateHWAHandler);
        
        var updateHWALengthHandler = new Com_UpdateHWALength();
        _updateHWALengthEvent = ExternalEvent.Create(updateHWALengthHandler);
        
        var splitPipeHandler = new Com_SplitPipe();
        _splitPipeEvent = ExternalEvent.Create(splitPipeHandler);
        
        
        
        var recessPlaceWallHandler = new Com_RecessPlaceWall();
        _recessPlaceWallEvent = ExternalEvent.Create(recessPlaceWallHandler);

        var recessPlaceBeamHandler = new Com_RecessPlaceBeam();
        _recessPlaceBeamEvent = ExternalEvent.Create(recessPlaceBeamHandler);

        var recessPlaceFloorHandler = new Com_RecessPlaceFloor();
        _recessPlaceFloorEvent = ExternalEvent.Create(recessPlaceFloorHandler);

        
        
        var prefabNewHandler = new Com_PrefabCreate();
        _prefabNewEvent = ExternalEvent.Create(prefabNewHandler);

        var prefabAddHandler = new Com_PrefabAdd();
        _prefabAddEvent = ExternalEvent.Create(prefabAddHandler);

        var prefabDeleteHandler = new Com_PrefabDelete();
        _prefabDeleteEvent = ExternalEvent.Create(prefabDeleteHandler);
        
        
        
        var tagMVLengthHandler = new Com_TagMVLength();
        _tagMVLengthEvent = ExternalEvent.Create(tagMVLengthHandler);
        
        var tagHWALengthHandler = new Com_TagHWALength();
        _tagHWALengthEvent = ExternalEvent.Create(tagHWALengthHandler);
        
        var tagVWALengthHandler = new Com_TagVWALength();
        _tagVWALengthEvent = ExternalEvent.Create(tagVWALengthHandler);
        
        var create3DViewHandler = new Com_Create3DView();
        _create3DViewEvent = ExternalEvent.Create(create3DViewHandler);
        
        var refreshViewHandler = new Com_RefreshView();
        _refreshViewEvent = ExternalEvent.Create(refreshViewHandler);
        
        
        
        ConnectElementCommand = new RelayCommands.RelayCommand(ExecuteConnectElement);
        ExportExcelCommand = new RelayCommands.RelayCommand(ExecuteExportExcel);
        UpdateHWACommand = new RelayCommands.RelayCommand(ExecuteUpdateHWA);
        UpdateHWALengthCommand = new RelayCommands.RelayCommand(ExecuteUpdateHWALength);
        SplitPipeCommand = new RelayCommands.RelayCommand(ExecuteSplitPipe);

        RecessPlaceWallCommand = new RelayCommands.RelayCommand(ExecuteRecessPlaceWall);
        RecessPlaceBeamCommand = new RelayCommands.RelayCommand(ExecuteRecessPlaceBeam);
        RecessPlaceFloorCommand = new RelayCommands.RelayCommand(ExecuteRecessPlaceFloor);
        
        PrefabNewCommand = new RelayCommands.RelayCommand(ExecutePrefabCreate);
        PrefabAddCommand = new RelayCommands.RelayCommand(ExecutePrefabAdd);
        PrefabDeleteCommand = new RelayCommands.RelayCommand(ExecutePrefabDelete);
        
        TagMVLengthCommand = new RelayCommands.RelayCommand(ExecuteTagMVLength);
        TagHWALengthCommand = new RelayCommands.RelayCommand(ExecuteTagHWALength);
        TagVWALengthCommand = new RelayCommands.RelayCommand(ExecuteTagVWALength);
        Create3DViewCommand = new RelayCommands.RelayCommand(ExecuteCreate3DView);
        RefreshViewCommand = new RelayCommands.RelayCommand(ExecuteRefreshView);
    }

    private void ExecuteConnectElement(object parameter)
    {
        _connectElementEvent.Raise();
    }

    private void ExecuteExportExcel(object parameter)
    {
        _exportExcelEvent.Raise();
    }

    private void ExecuteUpdateHWA(object parameter)
    {
        _updateHWAEvent.Raise();
    }
    
    private void ExecuteUpdateHWALength(object parameter)
    {
        _updateHWALengthEvent.Raise();
    }

    private void ExecuteSplitPipe(object parameter)
    {
        _splitPipeEvent.Raise();
    }
    
    
    
    private void ExecuteRecessPlaceWall(object parameter)
    {
        _recessPlaceWallEvent.Raise();
    }

    private void ExecuteRecessPlaceBeam(object parameter)
    {
        _recessPlaceBeamEvent.Raise();
    }

    private void ExecuteRecessPlaceFloor(object parameter)
    {
        _recessPlaceFloorEvent.Raise();
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
    
    
    
    private void ExecuteTagMVLength(object parameter)
    {
        _tagMVLengthEvent.Raise();
    }
    
    private void ExecuteTagHWALength(object parameter)
    {
        _tagHWALengthEvent.Raise();
    }
    
    private void ExecuteTagVWALength(object parameter)
    {
        _tagVWALengthEvent.Raise();
    }
    
    private void ExecuteCreate3DView(object parameter)
    {
        _create3DViewEvent.Raise();
    }
    
    private void ExecuteRefreshView(object parameter)
    {
        _refreshViewEvent.Raise();
    }
}