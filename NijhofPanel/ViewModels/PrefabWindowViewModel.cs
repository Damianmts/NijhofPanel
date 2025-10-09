namespace NijhofPanel.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Core;
using Helpers.Tools;
using NijhofPanel.Helpers.Core;

public class PrefabWindowViewModel : ObservableObject
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

    public PrefabWindowViewModel(
        RevitRequestHandler requestHandler,
        ExternalEvent externalEvent)
    {
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        _externalEvent = externalEvent ?? throw new ArgumentNullException(nameof(externalEvent));

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
        {
            ExecuteMaterialListScheduleCommand.Execute(set);
        }

        // Auto-save bij elke wijziging - alleen opslaan, niet opnieuw laden
        SaveDataOnly();
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

    // Nieuwe methode: alleen opslaan zonder de collectie te herladen
    public void SaveDataOnly()
    {
        if (RevitContext.UiApp == null || RevitContext.Uidoc?.Document == null)
        {
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
            return;
        }

        // Bepaal het pad naar het JSON-bestand (met debug-modus)
        string nijhofPanelPath;
        string prefabTekenPath;


        // Productie: opslaan op T:\Data
        var basePath = @"T:\Data";
        var invul1 = projectNummer.Length >= 2 ? projectNummer.Substring(0, 2) + "000" : "";
        var invul2 = projectNummer;
        prefabTekenPath = Path.Combine(basePath, invul1, invul2,
            "2.8 Tekeningen", "02 Nijhof", "03 PDF Prefab tekeningen");

        if (!Directory.Exists(prefabTekenPath))
        {
            return;
        }

        nijhofPanelPath = Path.Combine(prefabTekenPath, "Nijhof Panel");


        var jsonBestand = Path.Combine(nijhofPanelPath, "PrefabManager.json");

        // Seriële JSON-output van de huidige PrefabSets
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
                    GecontroleerdDatum = s.GecontroleerdDatum?.ToString("yyyy-MM-dd") ?? "",
                    s.Materiaallijst,
                    s.Zaaglijst
                }).ToList()
        };

        var jsonInhoud = JsonConvert.SerializeObject(saveData, Formatting.Indented);

        // Maak verborgen map "Nijhof Panel" aan als die nog niet bestaat
        if (!Directory.Exists(nijhofPanelPath))
        {
            Directory.CreateDirectory(nijhofPanelPath);
            // Maak de map verborgen
            var dirInfo = new DirectoryInfo(nijhofPanelPath);
            dirInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

        try
        {
            File.WriteAllText(jsonBestand, jsonInhoud);
        }
        catch
        {
            // Stil afhandelen bij auto-save
        }
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

        // Bepaal het pad naar het JSON-bestand (met debug-modus)
        string nijhofPanelPath;
        string prefabTekenPath;


        // Productie: opslaan op T:\Data
        var basePath = @"T:\Data";
        var invul1 = projectNummer.Length >= 2 ? projectNummer.Substring(0, 2) + "000" : "";
        var invul2 = projectNummer;
        prefabTekenPath = Path.Combine(basePath, invul1, invul2,
            "2.8 Tekeningen", "02 Nijhof", "03 PDF Prefab tekeningen");

        if (!Directory.Exists(prefabTekenPath))
        {
            MessageBox.Show($"Map bestaat niet:\n{prefabTekenPath}");
            return;
        }

        nijhofPanelPath = Path.Combine(prefabTekenPath, "Nijhof Panel");


        var jsonBestand = Path.Combine(nijhofPanelPath, "PrefabManager.json");

        // Sla eerst de huidige data op (indien er al sets zijn)
        if (PrefabSets.Any())
        {
            SaveDataOnly();
        }

        // Laad bestaande data indien aanwezig
        Dictionary<int, SavedPrefabSet>? bestaandeData = null;
        if (File.Exists(jsonBestand))
        {
            try
            {
                var jsonContent = File.ReadAllText(jsonBestand);
                var geladen = JsonConvert.DeserializeObject<SavedData>(jsonContent);
                if (geladen?.PrefabSets != null)
                {
                    bestaandeData = geladen.PrefabSets.ToDictionary(s => s.SetNumber, s => s);
                }
            }
            catch
            {
                // Als laden mislukt, gewoon doorgaan zonder bestaande data
            }
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

            var prefabSet = new PrefabSetHelper(_externalEvent, _requestHandler)
            {
                SetNumber = setNum,
                Discipline = discipline,
                ProjectNummer = projectNummer,
                HoofdTekenaar = hoofdTekenaar
            };

            // Vul gegevens in vanuit bestaande data indien beschikbaar
            if (bestaandeData != null && bestaandeData.ContainsKey(setNum))
            {
                var bestaand = bestaandeData[setNum];
                prefabSet.Bouwnummer = bestaand.Bouwnummer ?? "";
                prefabSet.Verdieping = bestaand.Verdieping ?? "";
                prefabSet.GecontroleerdNaam = bestaand.GecontroleerdNaam ?? "";

                // Parse datum string naar DateTime?
                if (!string.IsNullOrWhiteSpace(bestaand.GecontroleerdDatum)
                    && DateTime.TryParse(bestaand.GecontroleerdDatum, out var datum))
                {
                    prefabSet.GecontroleerdDatum = datum;
                }

                prefabSet.Materiaallijst = bestaand.Materiaallijst;
                prefabSet.Zaaglijst = bestaand.Zaaglijst;
            }

            PrefabSets.Add(prefabSet);
        }

        // Nu opslaan met de nieuwe data
        SaveDataOnly();

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
                    GecontroleerdDatum = s.GecontroleerdDatum?.ToString("yyyy-MM-dd") ?? "",
                    s.Materiaallijst,
                    s.Zaaglijst
                }).ToList()
        };

        var jsonInhoud = JsonConvert.SerializeObject(saveData, Formatting.Indented);

        // Maak verborgen map "Nijhof Panel" aan als die nog niet bestaat
        if (!Directory.Exists(nijhofPanelPath))
        {
            Directory.CreateDirectory(nijhofPanelPath);
            // Maak de map verborgen
            var dirInfo = new DirectoryInfo(nijhofPanelPath);
            dirInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

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

    private class SavedData
    {
        public string? ProjectNummer { get; set; }
        public string? HoofdTekenaar { get; set; }
        public List<SavedPrefabSet>? PrefabSets { get; set; }
    }

    private class SavedPrefabSet
    {
        public int SetNumber { get; set; }
        public string? Discipline { get; set; }
        public string? Verdieping { get; set; }
        public string? Bouwnummer { get; set; }
        public string? HoofdTekenaar { get; set; }
        public string? ProjectNummer { get; set; }
        public string? GecontroleerdNaam { get; set; }
        public string? GecontroleerdDatum { get; set; } // String ipv DateTime?
        public bool Materiaallijst { get; set; }
        public bool Zaaglijst { get; set; }
    }
}