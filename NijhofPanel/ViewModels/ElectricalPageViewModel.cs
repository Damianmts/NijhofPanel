namespace NijhofPanel.ViewModels;

using Helpers.Core;
using Helpers.Electrical;
using System.Windows.Input;
using Autodesk.Revit.UI;

public class ElectricalPageViewModel : ObservableObject
{
    private readonly FamilyPlacementHandler _handler;
    private readonly ExternalEvent _externalEvent;
    private readonly ExternalEvent _tagGroepnummerEvent;
    private readonly ExternalEvent _tagSwitchcodeEvent;
    private readonly ExternalEvent _codeLijstEvent;
    
    public ICommand PlaceElectricalComponentCommand { get; }
    public ICommand TagGroepnummerCommand { get; }
    public ICommand TagSwitchcodeCommand { get; }
    public ICommand CodeLijstCommand { get; }

    public ElectricalPageViewModel(FamilyPlacementHandler handler, ExternalEvent externalEvent,
        ExternalEvent tagGroepnummerEvent, ExternalEvent tagSwitchcodeEvent, ExternalEvent codeLijstEvent)
    {
        _handler = handler;
        _externalEvent = externalEvent;
        _tagGroepnummerEvent = tagGroepnummerEvent;
        _tagSwitchcodeEvent = tagSwitchcodeEvent;
        _codeLijstEvent = codeLijstEvent;

        PlaceElectricalComponentCommand = new RelayCommand<object>(OnPlaceElectricalComponent!);
        TagGroepnummerCommand = new RelayCommands.RelayCommand(ExecuteTagGroepnummer!);
        TagSwitchcodeCommand = new RelayCommands.RelayCommand(ExecuteTagSwitchcode!);
        CodeLijstCommand = new RelayCommands.RelayCommand(ExecuteCodeLijst!);
    }

    private void OnPlaceElectricalComponent(object componentType)
    {
        if (componentType is not string type) return;

        _handler.ComponentType = type;
        _externalEvent.Raise();
    }

    private void ExecuteTagGroepnummer(object parameter)
    {
        _tagGroepnummerEvent.Raise();
    }

    private void ExecuteTagSwitchcode(object parameter)
    {
        _tagSwitchcodeEvent.Raise();
    }
    
    private void ExecuteCodeLijst(object parameter)
    {
        _codeLijstEvent.Raise();
    }
}