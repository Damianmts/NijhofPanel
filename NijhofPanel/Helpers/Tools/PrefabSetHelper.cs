namespace NijhofPanel.Helpers.Tools;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;
using Core;

public class PrefabSetHelper : INotifyPropertyChanged
{
    private readonly ExternalEvent _externalEvent;
    private readonly RevitRequestHandler _requestHandler;

    public PrefabSetHelper(ExternalEvent externalEvent, RevitRequestHandler requestHandler)
    {
        _externalEvent = externalEvent ?? throw new ArgumentNullException(nameof(externalEvent));
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
    }

    private int _setNumber;

    public int SetNumber
    {
        get => _setNumber;
        set
        {
            if (_setNumber != value)
            {
                _setNumber = value;
                OnPropertyChanged();
            }
        }
    }

    private string _discipline = string.Empty;

    public string Discipline
    {
        get => _discipline;
        set
        {
            if (_discipline != value)
            {
                _discipline = value;
                OnPropertyChanged();
            }
        }
    }

    private string _verdieping = string.Empty;

    public string Verdieping
    {
        get => _verdieping;
        set
        {
            if (_verdieping != value)
            {
                _verdieping = value;
                OnPropertyChanged();
            }
        }
    }

    private string _bouwnummer = string.Empty;

    public string Bouwnummer
    {
        get => _bouwnummer;
        set
        {
            if (_bouwnummer != value)
            {
                _bouwnummer = value;
                OnPropertyChanged();
            }
        }
    }

    private string _gecontroleerdNaam = string.Empty;

    public string GecontroleerdNaam
    {
        get => _gecontroleerdNaam;
        set
        {
            if (_gecontroleerdNaam != value)
            {
                _gecontroleerdNaam = value;
                OnPropertyChanged();
            }
        }
    }

    private DateTime? _gecontroleerdDatum;

    public DateTime? GecontroleerdDatum
    {
        get => _gecontroleerdDatum;
        set
        {
            if (_gecontroleerdDatum != value)
            {
                _gecontroleerdDatum = value;
                OnPropertyChanged();
            }
        }
    }

    private string _projectNummer = string.Empty;

    public string ProjectNummer
    {
        get => _projectNummer;
        set
        {
            if (_projectNummer != value)
            {
                _projectNummer = value;
                OnPropertyChanged();
            }
        }
    }

    private string _hoofdTekenaar = string.Empty;

    public string HoofdTekenaar
    {
        get => _hoofdTekenaar;
        set
        {
            if (_hoofdTekenaar != value)
            {
                _hoofdTekenaar = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _materiaallijst;

    public bool Materiaallijst
    {
        get => _materiaallijst;
        set
        {
            if (_materiaallijst == value) return;
            _materiaallijst = value;
            OnPropertyChanged();
        }
    }

    private bool _zaaglijst;

    public bool Zaaglijst
    {
        get => _zaaglijst;
        set
        {
            if (_zaaglijst != value)
            {
                _zaaglijst = value;
                OnPropertyChanged();
                // Indien gewenst: ExecuteSawListSchedule();
            }
        }
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}