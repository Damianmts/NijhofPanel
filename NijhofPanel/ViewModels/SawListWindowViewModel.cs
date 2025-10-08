namespace NijhofPanel.ViewModels;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Commands.Tools;
using NijhofPanel.Helpers.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using Core;

public partial class SawListWindowViewModel : ObservableObject
{
    private readonly Document _doc;
    private readonly Window _window;
    
    private readonly RevitRequestHandler _requestHandler;
    private readonly ExternalEvent _externalEvent;

    public SawListWindowViewModel(Document doc, Window window,
                                  RevitRequestHandler requestHandler,
                                  ExternalEvent externalEvent)
    {
        _doc = doc;
        _window = window;
        _requestHandler = requestHandler;
        _externalEvent = externalEvent;

        LoadSawLists();
    }

    [ObservableProperty]
    private ObservableCollection<SawListItem> sawListItems = new();

    /// <summary>
    /// Haalt alle schedules op waarvan de naam 'zaaglijst' bevat.
    /// </summary>
    private void LoadSawLists()
    {
        var collector = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewSchedule))
            .Cast<ViewSchedule>()
            .Where(vs => !vs.IsTemplate &&
                         vs.Name.IndexOf("zaaglijst", StringComparison.OrdinalIgnoreCase) >= 0);

        var result = collector
            .Select(vs =>
            {
                string name = vs.Name;

                string discipline = ExtractDiscipline(name);
                string prefabset = ExtractPrefabset(name);
                string bouwnummer = ExtractBouwnummer(name);

                return new SawListItem
                {
                    IsSelected = false,
                    Discipline = discipline,
                    Prefabset = prefabset,
                    Bouwnummer = bouwnummer,
                    ViewId = vs.Id
                };
            });

        SawListItems = new ObservableCollection<SawListItem>(result);
    }

    // ---------------- COMMANDS ----------------

    [RelayCommand]
    private void Export()
    {
        var selected = SawListItems.Where(x => x.IsSelected).ToList();

        if (!selected.Any())
        {
            MessageBox.Show("Selecteer eerst één of meer zaaglijsten om te exporteren.",
                "Geen selectie",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (IsExcelSelected)
        {
            var schedules = selected
                .Select(item => _doc.GetElement(item.ViewId))
                .OfType<ViewSchedule>()
                .ToList();
            
            _requestHandler.Request = new RevitRequest(doc =>
            {
                var exporter = new Com_ExportExcelSawList(schedules);
                exporter.Execute(RevitContext.UiApp!);
            });

            _externalEvent.Raise();
        }
        else if (IsCsvSelected)
        {
            var schedules = selected
                .Select(item => _doc.GetElement(item.ViewId))
                .OfType<ViewSchedule>()
                .ToList();

            _requestHandler.Request = new RevitRequest(doc =>
            {
                var exporter = new Com_ExportCSV(schedules);
                exporter.Execute(RevitContext.UiApp!);
            });

            _externalEvent.Raise();
        }
    }

    [RelayCommand]
    private void Close() => _window?.Close();

    // ---------------- MODEL ----------------
    
    private static string ExtractDiscipline(string name)
    {
        var match = Regex.Match(name, @"\b(HWA|VWA|MV)\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpper() : string.Empty;
    }

    private static string ExtractPrefabset(string name)
    {
        var match = Regex.Match(name, @"\bSet\s*\d{1,2}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : string.Empty;
    }

    private static string ExtractBouwnummer(string name)
    {
        var match = Regex.Match(name, @"\bBNR\s*\d{1,3}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : string.Empty;
    }

    public partial class SawListItem : ObservableObject
    {
        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private string discipline = string.Empty;
        [ObservableProperty] private string prefabset = string.Empty;
        [ObservableProperty] private string bouwnummer = string.Empty;
        [ObservableProperty] private ElementId viewId;
    }

    // ---------------- EXPORTTYPE ----------------

    [ObservableProperty]
    private bool isCsvSelected = true;

    [ObservableProperty]
    private bool isExcelSelected = false;
}
