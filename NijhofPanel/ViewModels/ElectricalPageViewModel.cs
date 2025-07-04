namespace NijhofPanel.ViewModels;

using Helpers.Core;
using Helpers.Electrical;
using Commands.Electrical;
using System.Windows.Input;
using Autodesk.Revit.UI;

public class ElectricalPageViewModel
{
    private readonly FamilyPlacementHandler _handler;
    private readonly ExternalEvent _externalEvent;

    public static ElectricalPageViewModel? Instance { get; private set; }

    public ICommand PlaceElectricalComponentCommand { get; }
    public ICommand TagGroepnummerCommand { get; }
    public ICommand TagSwitchcodeCommand { get; }

    public ElectricalPageViewModel(FamilyPlacementHandler handler, ExternalEvent externalEvent)
    {
        _handler = handler;
        _externalEvent = externalEvent;

        PlaceElectricalComponentCommand = new RelayCommand<object>(OnPlaceElectricalComponent!);
        TagGroepnummerCommand = new RelayCommands.RelayCommand(ExecuteTagGroepnummer);
        TagSwitchcodeCommand = new RelayCommands.RelayCommand(ExecuteTagSwitchcode);

        Instance = this;
    }

    private void OnPlaceElectricalComponent(object componentType)
    {
        if (componentType is not string type) return;

        _handler.ComponentType = type;
        _externalEvent.Raise();
    }

    private void ExecuteTagGroepnummer(object parameter)
    {
        var command = new Com_TagGroepnummer();
        // Ik moet hier nog de juiste parameters doorgeven voor de Execute methode
        // command.Execute(...);
    }

    private void ExecuteTagSwitchcode(object parameter)
    {
        var command = new Com_TagSwitchcode();
        // Nog logica toevoegen
    }
}