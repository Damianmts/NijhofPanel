namespace NijhofPanel.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using NijhofPanel.Helpers;
using NijhofPanel.Services;
using NijhofPanel.Commands.Electrical;
using System.Collections.Generic;
using System.Windows.Input;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

public class ElectricalPageViewModel
{
    private readonly FamilyPlacementHandler _handler;
    private readonly ExternalEvent _externalEvent;

    public static ElectricalPageViewModel? Instance { get; private set; }

    public ICommand PlaceElectricalComponentCommand { get; }

    public ElectricalPageViewModel(FamilyPlacementHandler handler, ExternalEvent externalEvent)
    {
        _handler = handler;
        _externalEvent = externalEvent;

        PlaceElectricalComponentCommand = new RelayCommand<object>(OnPlaceElectricalComponent);

        Instance = this;
    }

    private void OnPlaceElectricalComponent(object componentType)
    {
        if (componentType is not string type) return;

        _handler.ComponentType = type;
        _externalEvent.Raise();
    }
}
