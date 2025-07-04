using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using NijhofPanel.Core;
using NijhofPanel.Helpers.Tools;
using NijhofPanel.Helpers.Core;

namespace NijhofPanel.ViewModels;

public class PrefabWindowViewModel : INotifyPropertyChanged
{
    private ObservableCollection<PrefabSetHelper> _prefabSets = new();

    public ICommand ExecuteMaterialListScheduleCommand { get; }

    public ObservableCollection<PrefabSetHelper> PrefabSets
    {
        get => _prefabSets;
        set
        {
            if (_prefabSets != value)
            {
                UnsubscribeCollection(_prefabSets);
                _prefabSets = value;
                SubscribeCollection(_prefabSets);
                OnPropertyChanged();
            }
        }
    }

    private readonly ExternalEvent _externalEvent;
    private readonly RevitRequestHandler _requestHandler;
    public static PrefabWindowViewModel? Instance { get; private set; }

    public PrefabWindowViewModel(
        RevitRequestHandler requestHandler,
        ExternalEvent externalEvent)
    {
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        _externalEvent = externalEvent ?? throw new ArgumentNullException(nameof(externalEvent));
        Instance = this;

        // Initialiseer command
        ExecuteMaterialListScheduleCommand = new RelayCommand<PrefabSetHelper>(OnExecuteMaterialListSchedule!);

        // Subscribe op de initiële collectie
        SubscribeCollection(PrefabSets);
    }

    private void SubscribeCollection(ObservableCollection<PrefabSetHelper> coll)
    {
        coll.CollectionChanged += PrefabSets_CollectionChanged;
        foreach (var set in coll)
            set.PropertyChanged += OnPrefabSetPropertyChanged;
    }

    private void UnsubscribeCollection(ObservableCollection<PrefabSetHelper> coll)
    {
        coll.CollectionChanged -= PrefabSets_CollectionChanged;
        foreach (var set in coll)
            set.PropertyChanged -= OnPrefabSetPropertyChanged;
    }

    private void PrefabSets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (PrefabSetHelper set in e.NewItems)
                set.PropertyChanged += OnPrefabSetPropertyChanged;

        if (e.OldItems != null)
            foreach (PrefabSetHelper set in e.OldItems)
                set.PropertyChanged -= OnPrefabSetPropertyChanged;
    }

    private void OnPrefabSetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PrefabSetHelper.Materiaallijst)
            && sender is PrefabSetHelper set
            && set.Materiaallijst)
            ExecuteMaterialListScheduleCommand.Execute(set);
    }

    private void OnExecuteMaterialListSchedule(PrefabSetHelper set)
    {
        var req = new RevitRequest(doc =>
        {
            var handler = new RevitScheduleHandler();
            handler.HandleMaterialListSchedule(
                doc,
                set.SetNumber.ToString(),
                set.Discipline);
        });

        _requestHandler.Request = req;
        _externalEvent.Raise();
    }

    public void AddNewPrefabSet(IEnumerable<RevitElementData> elements)
    {
        var newSetNumber = PrefabSets.Any() ? PrefabSets.Max(s => s.SetNumber) + 1 : 1;
        var firstElement = elements.FirstOrDefault();
        var systemAbbreviation = (firstElement?.Parameters!)
            .FirstOrDefault(p => p.Key == "System Abbreviation").Value;

        var prefabSet = new PrefabSetHelper(_externalEvent, _requestHandler)
        {
            SetNumber = newSetNumber,
            Discipline = systemAbbreviation != null ? DetermineDiscipline(systemAbbreviation) : "Onbekend"
        };

        PrefabSets.Add(prefabSet);
    }

    public void CollectAndSaveData()
    {
        if (RevitContext.UiApp == null || RevitContext.Uidoc?.Document == null)
        {
            MessageBox.Show("Geen actief Revit document gevonden.");
            return;
        }

        var doc = RevitContext.Uidoc.Document;

        // Lees project-metadata
        var projectInfo = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectInformation)
            .FirstElement();

        var projectNummer = "";
        var hoofdTekenaar = "";
        if (projectInfo != null)
        {
            var pNumParam = projectInfo.LookupParameter("Project Nummer");
            if (pNumParam?.HasValue == true) projectNummer = pNumParam.AsString() ?? "";
            var tekParam = projectInfo.LookupParameter("Hoofd tekenaar");
            if (tekParam?.HasValue == true) hoofdTekenaar = tekParam.AsString() ?? "";
        }

        if (string.IsNullOrEmpty(projectNummer))
        {
            MessageBox.Show("Geen projectnummer gevonden.");
            return;
        }

        // Verzamel prefab-elementen
        var prefabCats = new[]
        {
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_DuctFitting
        };

        var allElems = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            .ToElements();

        var prefabGroups = allElems
            .Where(e => e.Category != null &&
                        prefabCats.Contains((BuiltInCategory)e.Category.Id.Value) &&
                        e.LookupParameter("Prefab Set")?.HasValue == true)
            .GroupBy(e => e.LookupParameter("Prefab Set")!.AsString())
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        PrefabSets.Clear();
        foreach (var group in prefabGroups.OrderBy(g => int.TryParse(g.Key, out var n) ? n : int.MaxValue))
        {
            var setNum = int.TryParse(group.Key, out var n) ? n : 0;
            var sysAbb = group.First().LookupParameter("System Abbreviation")?.AsString() ?? "";
            var discipline = DetermineDiscipline(sysAbb);

            PrefabSets.Add(new PrefabSetHelper(_externalEvent, _requestHandler)
            {
                SetNumber = setNum,
                Discipline = discipline,
                ProjectNummer = projectNummer,
                HoofdTekenaar = hoofdTekenaar
            });
        }

        // Seriële JSON-output
        var saveData = new
        {
            ProjectNummer = projectNummer,
            HoofdTekenaar = hoofdTekenaar,
            PrefabSets = PrefabSets.OrderBy(s => s.SetNumber)
                .Select(s => new
                {
                    s.SetNumber,
                    s.Discipline,
                    s.Verdieping,
                    s.Bouwnummer,
                    s.HoofdTekenaar,
                    s.ProjectNummer,
                    s.GecontroleerdNaam,
                    s.GecontroleerdDatum,
                    s.Materiaallijst,
                    s.Zaaglijst
                }).ToList()
        };

        var jsonInhoud = JsonConvert.SerializeObject(saveData, Formatting.Indented);

        var basePath = @"T:\Data";
        var invul1 = projectNummer.Length >= 2 ? projectNummer.Substring(0, 2) + "000" : "";
        var invul2 = projectNummer;
        var fullPath = Path.Combine(basePath, invul1, invul2,
            "2.8 Tekeningen", "02 Nijhof", "03 PDF Prefab tekeningen");

        if (!Directory.Exists(fullPath))
        {
            MessageBox.Show($"Map bestaat niet:\n{fullPath}");
            return;
        }

        var jsonBestand = Path.Combine(fullPath, "PrefabManager.json");
        try
        {
            File.WriteAllText(jsonBestand, jsonInhoud);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij opslaan:\n{ex.Message}");
        }
    }

    private string DetermineDiscipline(string systemAbbreviation)
    {
        if (systemAbbreviation.StartsWith("M524")) return "VWA";
        if (systemAbbreviation.StartsWith("M521")) return "HWA";
        if (systemAbbreviation.StartsWith("M57")) return "MV";
        return "Onbekend";
    }

    public class RevitElementData
    {
        public string? ElementId { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}