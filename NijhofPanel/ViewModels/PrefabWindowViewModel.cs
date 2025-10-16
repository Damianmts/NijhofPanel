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
    // Velden
    private ObservableCollection<PrefabSetHelper> _prefabSets = new();
    private readonly ExternalEvent _externalEvent;
    private readonly RevitRequestHandler _requestHandler;
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private bool _isListeningForRevitChanges;
    private bool _isLoadingData = false;
    private bool _isRefreshingFromRevit = false;

    // Constructor
    public PrefabWindowViewModel(RevitRequestHandler requestHandler, ExternalEvent externalEvent)
    {
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        _externalEvent = externalEvent ?? throw new ArgumentNullException(nameof(externalEvent));

        // Dispatcher opslaan
        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

        ExecuteMaterialListScheduleCommand = new RelayCommand<PrefabSetHelper>(OnExecuteMaterialListSchedule!);
        DeletePrefabSetCommand = new RelayCommand<PrefabSetHelper>(OnDeletePrefabSet!);

        SubscribeCollection(PrefabSets);
    }


    // Properties
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

    // Commands
    public ICommand ExecuteMaterialListScheduleCommand { get; }
    public ICommand DeletePrefabSetCommand { get; }

    // Revit Change Listener
    public void StartRevitChangeListener()
    {
        if (_isListeningForRevitChanges)
            return;

        ChangeWatcher.ScheduleChanged += OnRevitScheduleChanged;
        _isListeningForRevitChanges = true;
    }

    public void StopRevitChangeListener()
    {
        if (!_isListeningForRevitChanges)
            return;

        ChangeWatcher.ScheduleChanged -= OnRevitScheduleChanged;
        _isListeningForRevitChanges = false;
    }

    private void OnRevitScheduleChanged() => RefreshScheduleStatusFromRevit();

    // Collectiebeheer
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

    // PropertyChanged events
    private void OnPrefabSetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoadingData)
            return; // Niets doen tijdens laden

        if (sender is not PrefabSetHelper set)
            return;

        if (e.PropertyName == nameof(PrefabSetHelper.Materiaallijst))
        {
            if (set.Materiaallijst)
                ExecuteMaterialListScheduleCommand.Execute(set);
            else
                OnDeleteMaterialListSchedule(set);
        }
        else if (e.PropertyName == nameof(PrefabSetHelper.Zaaglijst))
        {
            if (set.Zaaglijst)
                OnExecuteCutListSchedule(set);
            else
                OnDeleteCutListSchedule(set);
        }

        // Auto-save bij elke wijziging
        SaveDataOnly();
    }

    // Revit-acties (Schedules genereren)
    private void OnExecuteMaterialListSchedule(PrefabSetHelper set)
    {
        var req = new RevitRequest(doc =>
        {
            var handler = new RevitScheduleHandler();
            handler.HandleMaterialListSchedule(doc, set.SetNumber.ToString(), set.Discipline, set.Bouwnummer);
        });

        _requestHandler.Request = req;
        _externalEvent.Raise();
    }

    private void OnExecuteCutListSchedule(PrefabSetHelper set)
    {
        var req = new RevitRequest(doc =>
        {
            var handler = new RevitScheduleHandler();
            handler.HandleSawListSchedule(doc, set.SetNumber.ToString(), set.Discipline, set.Bouwnummer);
        });

        _requestHandler.Request = req;
        _externalEvent.Raise();
    }

    // Data-opslag en -laden (JSON)
    public void SaveDataOnly()
    {
        if (RevitContext.UiApp == null || RevitContext.Uidoc?.Document == null)
            return;

        var doc = RevitContext.Uidoc.Document;

        // Metadata ophalen
        var projectInfo = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectInformation)
            .FirstElement();

        var projectNummer = projectInfo?.LookupParameter("Project Nummer")?.AsString() ?? "";
        var hoofdTekenaar = projectInfo?.LookupParameter("Hoofd tekenaar")?.AsString() ?? "";

        if (string.IsNullOrEmpty(projectNummer))
            return;

        // JSON-data aanmaken
        var saveData = new
        {
            ProjectNummer = projectNummer,
            HoofdTekenaar = hoofdTekenaar,
            PrefabSets = PrefabSets
                .OrderBy(s => s.SetNumber)
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

        try
        {
            PrefabManagerHelper.SavePrefabManagerObject(projectNummer, saveData, "PrefabSets");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij opslaan van PrefabManager.json:\n{ex.Message}",
                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void CollectAndSaveData()
    {
        _isLoadingData = true;

        try
        {
            if (RevitContext.UiApp == null || RevitContext.Uidoc?.Document == null)
            {
                MessageBox.Show("Geen actief Revit document gevonden.");
                return;
            }

            var doc = RevitContext.Uidoc.Document;
            var projectInfo = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_ProjectInformation)
                .FirstElement();

            var projectNummer = projectInfo?.LookupParameter("Project Nummer")?.AsString() ?? "";
            var hoofdTekenaar = projectInfo?.LookupParameter("Hoofd tekenaar")?.AsString() ?? "";

            if (string.IsNullOrEmpty(projectNummer))
            {
                MessageBox.Show("Geen projectnummer gevonden.");
                return;
            }

            if (PrefabSets.Any())
                SaveDataOnly();

            string jsonBestand;
            try
            {
                jsonBestand = PrefabManagerHelper.EnsurePrefabManagerPath(projectNummer);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kan geen pad bepalen voor PrefabManager.json:\n{ex.Message}");
                return;
            }

            // Bestaande data laden
            Dictionary<int, SavedPrefabSet>? bestaandeData = null;
            if (File.Exists(jsonBestand))
            {
                try
                {
                    var jsonContent = File.ReadAllText(jsonBestand);
                    var root = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);

                    // Probeer de nested sectie "PrefabSets" te vinden
                    var prefabSetsToken = root?["PrefabSets"]?["PrefabSets"];
                    if (prefabSetsToken != null)
                    {
                        var sets = prefabSetsToken.ToObject<List<SavedPrefabSet>>();
                        bestaandeData = sets?.ToDictionary(s => s.SetNumber, s => s);
                    }
                }
                catch
                {
                    // JSON is corrupt of leeg → negeren
                }
            }

            // Alleen relevante categorieën ophalen
            var prefabCats = new[]
            {
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_DuctFitting
            };

            var allElems = prefabCats
                .SelectMany(cat =>
                    new FilteredElementCollector(doc)
                        .OfCategory(cat)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent()
                        .ToElements())
                .ToList();

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
                var abbreviations = group.Select(e => e.LookupParameter("System Abbreviation")?.AsString())
                    .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                var sysAbb = abbreviations.GroupBy(s => s)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key).FirstOrDefault() ?? "";

                var discipline = DetermineDiscipline(sysAbb);
                var bouwnummer = GetParameterValue(group, "Prefab Kavelnummer", "Kavelnummer") ?? "";
                var verdieping = GetParameterValue(group, "Prefab Verdieping", "Verdieping") ?? "";

                var prefabSet = new PrefabSetHelper(_externalEvent, _requestHandler)
                {
                    SetNumber = setNum,
                    Discipline = discipline,
                    ProjectNummer = projectNummer,
                    HoofdTekenaar = hoofdTekenaar,
                    Bouwnummer = bouwnummer,
                    Verdieping = verdieping
                };

                if (bestaandeData != null && bestaandeData.ContainsKey(setNum))
                {
                    var bestaand = bestaandeData[setNum];
                    prefabSet.Bouwnummer = bestaand.Bouwnummer ?? "";
                    prefabSet.Verdieping = bestaand.Verdieping ?? "";
                    prefabSet.GecontroleerdNaam = bestaand.GecontroleerdNaam ?? "";
                    if (DateTime.TryParse(bestaand.GecontroleerdDatum, out var datum))
                        prefabSet.GecontroleerdDatum = datum;
                    prefabSet.Materiaallijst = bestaand.Materiaallijst;
                    prefabSet.Zaaglijst = bestaand.Zaaglijst;
                }

                PrefabSets.Add(prefabSet);
            }

            // Schedules opnieuw synchroniseren
            RefreshScheduleStatusFromRevit();

            // Gegevens opslaan
            SaveDataOnly();
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    // Hulp- en hulpmethoden
    private static string? GetParameterValue(IGrouping<string, Autodesk.Revit.DB.Element> group,
        params string[] paramNames)
    {
        foreach (var elem in group)
        {
            var match = elem.Parameters.Cast<Autodesk.Revit.DB.Parameter>()
                .FirstOrDefault(p => paramNames.Any(name =>
                    p.Definition.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

            if (match?.HasValue == true)
            {
                var val = match.AsString();
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }
        }

        return null;
    }

    private string DetermineDiscipline(string systemAbbreviation)
    {
        if (systemAbbreviation.StartsWith("M524")) return "VWA";
        if (systemAbbreviation.StartsWith("M521")) return "HWA";
        if (systemAbbreviation.StartsWith("M57")) return "MV";
        return "Onbekend";
    }

    public void RefreshScheduleStatusFromRevit()
    {
        if (RevitContext.UiApp == null || RevitContext.Uidoc?.Document == null)
            return;

        var doc = RevitContext.Uidoc.Document;

        var allSchedules = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Schedules)
            .WhereElementIsNotElementType()
            .ToElements();

        _isRefreshingFromRevit = true; // Zet lock aan

        try
        {
            foreach (var set in PrefabSets)
            {
                var setNum = set.SetNumber;

                bool hasMaterialList = allSchedules.Any(s =>
                {
                    var name = s.Name?.ToLowerInvariant() ?? "";
                    return name.Contains($"set {setNum}") && name.Contains("materiaal");
                });

                bool hasCutList = allSchedules.Any(s =>
                {
                    var name = s.Name?.ToLowerInvariant() ?? "";
                    return name.Contains($"set {setNum}") && name.Contains("zaag");
                });

                // ✳Alleen bijwerken zonder events te triggeren
                if (set.Materiaallijst != hasMaterialList)
                    set.Materiaallijst = hasMaterialList;

                if (set.Zaaglijst != hasCutList)
                    set.Zaaglijst = hasCutList;
            }
        }
        finally
        {
            _isRefreshingFromRevit = false; // Zet lock uit
        }
    }

    // Verwijderen van Materiaallijst schedule
    private void OnDeleteMaterialListSchedule(PrefabSetHelper set)
    {
        var confirm = MessageBox.Show(
            $"Weet je zeker dat je de materiaallijst van Prefab Set {set.SetNumber} wilt verwijderen?",
            "Bevestiging vereist",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            // Zet checkbox terug op true als gebruiker annuleert
            set.Materiaallijst = true;
            return;
        }

        var req = new RevitRequest(doc =>
        {
            try
            {
                var schedules = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Schedules)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .Where(e =>
                    {
                        var name = e.Name?.ToLowerInvariant() ?? "";
                        return name.Contains($"set {set.SetNumber}") && name.Contains("materiaal");
                    })
                    .Select(e => e.Id)
                    .ToList();

                if (schedules.Any())
                {
                    using var t = new Autodesk.Revit.DB.Transaction(doc,
                        $"Verwijder materiaallijst (Set {set.SetNumber})");
                    t.Start();
                    doc.Delete(schedules);
                    t.Commit();
                }
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fout bij verwijderen van materiaallijst:\n{ex.Message}",
                        "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    set.Materiaallijst = true; // herstellen bij fout
                });
            }
        });

        _requestHandler.Request = req;
        _externalEvent.Raise();
    }

    // Verwijderen van Zaaglijst schedule
    private void OnDeleteCutListSchedule(PrefabSetHelper set)
    {
        var confirm = MessageBox.Show(
            $"Weet je zeker dat je de zaaglijst van Prefab Set {set.SetNumber} wilt verwijderen?",
            "Bevestiging vereist",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            set.Zaaglijst = true;
            return;
        }

        var req = new RevitRequest(doc =>
        {
            try
            {
                var schedules = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Schedules)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .Where(e =>
                    {
                        var name = e.Name?.ToLowerInvariant() ?? "";
                        return name.Contains($"set {set.SetNumber}") && name.Contains("zaag");
                    })
                    .Select(e => e.Id)
                    .ToList();

                if (schedules.Any())
                {
                    using var t = new Autodesk.Revit.DB.Transaction(doc,
                        $"Verwijder zaaglijst (Set {set.SetNumber})");
                    t.Start();
                    doc.Delete(schedules);
                    t.Commit();
                }
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fout bij verwijderen van zaaglijst:\n{ex.Message}",
                        "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    set.Zaaglijst = true; // herstellen bij fout
                });
            }
        });

        _requestHandler.Request = req;
        _externalEvent.Raise();
    }

    // Verwijderen van PrefabSets
    private void OnDeletePrefabSet(PrefabSetHelper set)
    {
        if (set == null)
        {
            MessageBox.Show("Geen prefabset geselecteerd om te verwijderen.");
            return;
        }

        if (_requestHandler == null || _externalEvent == null)
        {
            MessageBox.Show("Kan Prefab Set niet verwijderen: Revit event handler is niet geïnitialiseerd.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Weet je zeker dat je Prefab Set {set.SetNumber} wilt verwijderen?",
            "Bevestiging vereist",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        // Store set number for use in callback
        var setNumber = set.SetNumber;

        var req = new RevitRequest(doc =>
        {
            if (doc == null)
            {
                _dispatcher.Invoke(() => { MessageBox.Show("Document is null - kan Prefab Set niet verwijderen."); });
                return;
            }

            try
            {
                // Alle elementen met deze Prefab Set zoeken
                var elementsWithSet = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .ToElements()
                    .Where(e =>
                    {
                        var p = e.LookupParameter("Prefab Set");
                        if (p == null) return false;
                        var val = p.AsString();
                        return !string.IsNullOrWhiteSpace(val) && val.Equals(setNumber.ToString());
                    })
                    .ToList();

                if (elementsWithSet.Any())
                {
                    using var t = new Autodesk.Revit.DB.Transaction(doc, $"Prefab Set-gegevens wissen ({setNumber})");
                    t.Start();

                    foreach (var e in elementsWithSet)
                    {
                        // Alleen parameters leegmaken (niet verwijderen)
                        void ClearParam(string name)
                        {
                            var p = e.LookupParameter(name);
                            if (p != null && !p.IsReadOnly)
                            {
                                try
                                {
                                    if (p.StorageType == Autodesk.Revit.DB.StorageType.String)
                                        p.Set(string.Empty);
                                    else if (p.StorageType == Autodesk.Revit.DB.StorageType.Integer)
                                        p.Set(0);
                                }
                                catch
                                {
                                    /* negeren als parameter niet overschrijfbaar is */
                                }
                            }
                        }

                        // Meest gebruikte prefab-parameters:
                        ClearParam("Prefab Color ID");
                        ClearParam("Prefab Set");
                        ClearParam("Prefab Kavelnummer");
                        ClearParam("Kavelnummer");
                        ClearParam("Prefab Verdieping");
                        ClearParam("Verdieping");
                    }

                    t.Commit();
                }

                // Bijbehorende schedules verwijderen
                var schedules = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Schedules)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .Where(e => (e.Name?.ToLowerInvariant() ?? "").Contains($"set {setNumber}"))
                    .Select(e => e.Id)
                    .ToList();

                if (schedules.Any())
                {
                    using var t2 =
                        new Autodesk.Revit.DB.Transaction(doc, $"Verwijder Prefab Set {setNumber} schedules");
                    t2.Start();
                    doc.Delete(schedules);
                    t2.Commit();
                }

                // Remove from collection AFTER successful Revit operations
                _dispatcher.Invoke(() =>
                {
                    var setToRemove = PrefabSets.FirstOrDefault(s => s.SetNumber == setNumber);
                    if (setToRemove != null)
                    {
                        PrefabSets.Remove(setToRemove);
                        SaveDataOnly();
                    }
                });
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Er trad een fout op bij het verwijderen van Prefab Set:\n{ex.Message}",
                        "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        });

        try
        {
            _requestHandler.Request = req;
            _externalEvent.Raise();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Er trad een fout op bij het uitvoeren van de Revit-actie:\n{ex.Message}",
                "Revit Event Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Interne klassen
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
        public string? GecontroleerdDatum { get; set; }
        public bool Materiaallijst { get; set; }
        public bool Zaaglijst { get; set; }
    }
}