namespace NijhofPanel.ViewModels;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core;
using Helpers.Core;
using Visibility = System.Windows.Visibility;

public class SettingsPageViewModel : ObservableObject
{
    // --- Prefab Template configuratie ---

    // --- Beschikbare templates (gevuld vanuit Revit) ---
    public ObservableCollection<string> AvailableViewTemplates { get; } = new();
    public ObservableCollection<string> AvailablePlanTemplates { get; } = new();
    public ObservableCollection<string> Available3DTemplates { get; } = new();

    public class TemplateSet
    {
        public string Plan { get; set; } = "";
        public string Maatvoering { get; set; } = "";
        public string View3D { get; set; } = "";
    }

    public class PrefabTemplateSettings
    {
        public TemplateSet M524 { get; set; } = new();
        public TemplateSet M521 { get; set; } = new();
        public TemplateSet M570 { get; set; } = new();
    }

    private const string SettingsFileName = "ArtikelnummerSettings.json";

    private string _networkPath = null!;
    private Dictionary<string, Dictionary<string, string>> _artikelnummerData = null!;

    private string _selectedProductType = null!;
    private string _diameter40 = null!;
    private string _diameter50 = null!;
    private string _diameter75 = null!;
    private string _diameter80 = null!;
    private string _diameter90 = null!;
    private string _diameter110 = null!;
    private string _diameter125 = null!;
    private string _diameter160 = null!;
    private string _diameter19580 = null!;
    private string _diameter23580 = null!;

    public ObservableCollection<ProductTypeItem> ProductTypes { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SavePrefabTemplateCommand { get; }

    public string SelectedProductType
    {
        get => _selectedProductType;
        set
        {
            if (SetProperty(ref _selectedProductType, value))
            {
                UpdateVisibleDiameters();
                LoadCurrentSelection();
            }
        }
    }

    // Diameter properties
    public string? Diameter40
    {
        get => _diameter40;
        set => SetProperty(ref _diameter40!, value);
    }

    public string? Diameter50
    {
        get => _diameter50;
        set => SetProperty(ref _diameter50!, value);
    }

    public string? Diameter75
    {
        get => _diameter75;
        set => SetProperty(ref _diameter75!, value);
    }

    public string? Diameter80
    {
        get => _diameter80;
        set => SetProperty(ref _diameter80!, value);
    }

    public string? Diameter90
    {
        get => _diameter90;
        set => SetProperty(ref _diameter90!, value);
    }

    public string? Diameter110
    {
        get => _diameter110;
        set => SetProperty(ref _diameter110!, value);
    }

    public string? Diameter125
    {
        get => _diameter125;
        set => SetProperty(ref _diameter125!, value);
    }

    public string? Diameter160
    {
        get => _diameter160;
        set => SetProperty(ref _diameter160!, value);
    }

    public string? Diameter19580
    {
        get => _diameter19580;
        set => SetProperty(ref _diameter19580!, value);
    }

    public string? Diameter23580
    {
        get => _diameter23580;
        set => SetProperty(ref _diameter23580!, value);
    }

    // Visibility properties
    private Visibility _diameter40Visibility;
    private Visibility _diameter50Visibility;
    private Visibility _diameter75Visibility;
    private Visibility _diameter80Visibility;
    private Visibility _diameter90Visibility;
    private Visibility _diameter110Visibility;
    private Visibility _diameter125Visibility;
    private Visibility _diameter160Visibility;
    private Visibility _diameter19580Visibility;
    private Visibility _diameter23580Visibility;

    public Visibility Diameter40Visibility
    {
        get => _diameter40Visibility;
        set => SetProperty(ref _diameter40Visibility, value);
    }

    public Visibility Diameter50Visibility
    {
        get => _diameter50Visibility;
        set => SetProperty(ref _diameter50Visibility, value);
    }

    public Visibility Diameter75Visibility
    {
        get => _diameter75Visibility;
        set => SetProperty(ref _diameter75Visibility, value);
    }

    public Visibility Diameter80Visibility
    {
        get => _diameter80Visibility;
        set => SetProperty(ref _diameter80Visibility, value);
    }

    public Visibility Diameter90Visibility
    {
        get => _diameter90Visibility;
        set => SetProperty(ref _diameter90Visibility, value);
    }

    public Visibility Diameter110Visibility
    {
        get => _diameter110Visibility;
        set => SetProperty(ref _diameter110Visibility, value);
    }

    public Visibility Diameter125Visibility
    {
        get => _diameter125Visibility;
        set => SetProperty(ref _diameter125Visibility, value);
    }

    public Visibility Diameter160Visibility
    {
        get => _diameter160Visibility;
        set => SetProperty(ref _diameter160Visibility, value);
    }

    public Visibility Diameter19580Visibility
    {
        get => _diameter19580Visibility;
        set => SetProperty(ref _diameter19580Visibility, value);
    }

    public Visibility Diameter23580Visibility
    {
        get => _diameter23580Visibility;
        set => SetProperty(ref _diameter23580Visibility, value);
    }

    // --- M524 ---
    public string SelectedM524Plan
    {
        get => _m524Plan;
        set => SetProperty(ref _m524Plan, value);
    }

    public string SelectedM524Maatvoering
    {
        get => _m524Maatvoering;
        set => SetProperty(ref _m524Maatvoering, value);
    }

    public string SelectedM5243D
    {
        get => _m5243D;
        set => SetProperty(ref _m5243D, value);
    }

    private string _m524Plan = "P52_Riolering";
    private string _m524Maatvoering = "P50_Lucht_Riolering_Maatvoering";
    private string _m5243D = "3D_P52_Riolering";

    // --- M521 ---
    public string SelectedM521Plan
    {
        get => _m521Plan;
        set => SetProperty(ref _m521Plan, value);
    }

    public string SelectedM521Maatvoering
    {
        get => _m521Maatvoering;
        set => SetProperty(ref _m521Maatvoering, value);
    }

    public string SelectedM5213D
    {
        get => _m5213D;
        set => SetProperty(ref _m5213D, value);
    }

    private string _m521Plan = "P52_Riolering";
    private string _m521Maatvoering = "";
    private string _m5213D = "3D_P52_Riolering";

    // --- M570 ---
    public string SelectedM570Plan
    {
        get => _m570Plan;
        set => SetProperty(ref _m570Plan, value);
    }

    public string SelectedM570Maatvoering
    {
        get => _m570Maatvoering;
        set => SetProperty(ref _m570Maatvoering, value);
    }

    public string SelectedM5703D
    {
        get => _m5703D;
        set => SetProperty(ref _m5703D, value);
    }

    private string _m570Plan = "P57_Lucht";
    private string _m570Maatvoering = "P50_Lucht_Riolering_Maatvoering";
    private string _m5703D = "3D_P57_Lucht";


    public SettingsPageViewModel()
    {
        ProductTypes = new ObservableCollection<ProductTypeItem>
        {
            // Riool sectie
            new ProductTypeItem { DisplayName = "Dyka PVC", Tag = "DykaPVC" },
            new ProductTypeItem { DisplayName = "Dyka Sono", Tag = "DykaSono" },
            new ProductTypeItem { DisplayName = "Dyka HWA", Tag = "DykaHWA" },
            new ProductTypeItem { DisplayName = "───────────────", Tag = "Separator1", IsEnabled = false },

            // Lucht sectie
            new ProductTypeItem { DisplayName = "Dyka Air", Tag = "DykaAir" }
        };

        SaveCommand = new RelayCommand(ExecuteSave);

        SavePrefabTemplateCommand = new RelayCommand(() =>
        {
            var doc = RevitContext.Uidoc?.Document;
            string projectNummer = ProjectInfoHelper.GetProjectNummer(doc);

            if (string.IsNullOrWhiteSpace(projectNummer))
            {
                MessageBox.Show("Projectnummer niet gevonden in Revit.", "Fout",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SavePrefabTemplateSettings(projectNummer);
        });

        InitializeArtikelnummerSettings();

        // Laad alle view templates uit Revit
        LoadAvailableViewTemplates();

        var doc = RevitContext.Uidoc?.Document;
        string projectNummer = ProjectInfoHelper.GetProjectNummer(doc);

        // Laad de instellingen (of gebruik fallback waarden)
        var prefabSettings = LoadPrefabTemplateSettings(projectNummer);

        // Helper functie om de beste match te vinden
        string FindBestMatch(string preferred, string fallback, ObservableCollection<string> availableList)
        {
            // Check eerst of de preferred waarde bestaat
            if (availableList.Contains(preferred))
                return preferred;

            // Check dan de fallback waarde
            if (availableList.Contains(fallback))
                return fallback;

            // Als beide niet bestaan, return de eerste niet-lege waarde of lege string
            return "";
        }

        // M524 - Riolering
        SelectedM524Plan = FindBestMatch(
            prefabSettings.M524.Plan,
            "02_Prefab_Riolering_Prefab",
            AvailablePlanTemplates);

        SelectedM524Maatvoering = FindBestMatch(
            prefabSettings.M524.Maatvoering,
            "02_Prefab_Riolering_Maatvoering",
            AvailablePlanTemplates);

        SelectedM5243D = FindBestMatch(
            prefabSettings.M524.View3D,
            "04_Plot_Prefab_Riool_3D",
            Available3DTemplates);

        // M521 - Hemelwater
        SelectedM521Plan = FindBestMatch(
            prefabSettings.M521.Plan,
            "02_Prefab_Riolering_Prefab",
            AvailablePlanTemplates);

        SelectedM521Maatvoering = FindBestMatch(
            prefabSettings.M521.Maatvoering,
            "",
            AvailablePlanTemplates);

        SelectedM5213D = FindBestMatch(
            prefabSettings.M521.View3D,
            "04_Plot_Prefab_Riool_3D",
            Available3DTemplates);

        // M570 - Lucht
        SelectedM570Plan = FindBestMatch(
            prefabSettings.M570.Plan,
            "02_Prefab_Lucht_Prefab",
            AvailablePlanTemplates);

        SelectedM570Maatvoering = FindBestMatch(
            prefabSettings.M570.Maatvoering,
            "02_Prefab_Lucht_Maatvoering",
            AvailablePlanTemplates);

        SelectedM5703D = FindBestMatch(
            prefabSettings.M570.View3D,
            "03_Plot_Prefab_Lucht_3D",
            Available3DTemplates);

        SelectedProductType = "DykaPVC";
    }

    private void InitializeArtikelnummerSettings()
    {
        _networkPath = @"F:\Revit\Plugin\Nijhof Panel\Data";

        if (!Directory.Exists(_networkPath))
        {
            Directory.CreateDirectory(_networkPath);
        }

        _artikelnummerData = new Dictionary<string, Dictionary<string, string>>();
        LoadSettings();
    }

    private void UpdateVisibleDiameters()
    {
        if (string.IsNullOrEmpty(SelectedProductType))
            return;

        bool isHWA = SelectedProductType == "DykaHWA";
        bool isSono = SelectedProductType == "DykaSono";
        bool isPVC = SelectedProductType == "DykaPVC";
        bool isAir = SelectedProductType == "DykaAir";

        Diameter40Visibility = isPVC ? Visibility.Visible : Visibility.Collapsed;
        Diameter50Visibility = (isPVC || isSono) ? Visibility.Visible : Visibility.Collapsed;
        Diameter75Visibility = (isPVC || isSono) ? Visibility.Visible : Visibility.Collapsed;
        Diameter80Visibility = (isHWA || isAir) ? Visibility.Visible : Visibility.Collapsed;
        Diameter90Visibility = (isPVC || isSono) ? Visibility.Visible : Visibility.Collapsed;
        Diameter110Visibility = (isPVC || isSono) ? Visibility.Visible : Visibility.Collapsed;
        Diameter125Visibility = (isPVC || isSono || isAir) ? Visibility.Visible : Visibility.Collapsed;
        Diameter160Visibility = (isPVC || isSono || isAir) ? Visibility.Visible : Visibility.Collapsed;
        Diameter19580Visibility = isAir ? Visibility.Visible : Visibility.Collapsed;
        Diameter23580Visibility = isAir ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadCurrentSelection()
    {
        if (string.IsNullOrEmpty(SelectedProductType))
            return;

        if (_artikelnummerData.ContainsKey(SelectedProductType))
        {
            var data = _artikelnummerData[SelectedProductType];
            Diameter40 = data.ContainsKey("40") ? data["40"] : "";
            Diameter50 = data.ContainsKey("50") ? data["50"] : "";
            Diameter75 = data.ContainsKey("75") ? data["75"] : "";
            Diameter80 = data.ContainsKey("80") ? data["80"] : "";
            Diameter90 = data.ContainsKey("90") ? data["90"] : "";
            Diameter110 = data.ContainsKey("110") ? data["110"] : "";
            Diameter125 = data.ContainsKey("125") ? data["125"] : "";
            Diameter160 = data.ContainsKey("160") ? data["160"] : "";
            Diameter19580 = data.ContainsKey("195/80") ? data["195/80"] : "";
            Diameter23580 = data.ContainsKey("235/80") ? data["235/80"] : "";
        }
        else
        {
            ClearAllFields();
        }
    }

    private void ClearAllFields()
    {
        Diameter40 = "";
        Diameter50 = "";
        Diameter75 = "";
        Diameter80 = "";
        Diameter90 = "";
        Diameter110 = "";
        Diameter125 = "";
        Diameter160 = "";
        Diameter19580 = "";
        Diameter23580 = "";
    }

    private void ExecuteSave()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedProductType))
                return;

            var diameters = new Dictionary<string, string>
            {
                ["40"] = Diameter40 ?? "",
                ["50"] = Diameter50 ?? "",
                ["75"] = Diameter75 ?? "",
                ["80"] = Diameter80 ?? "",
                ["90"] = Diameter90 ?? "",
                ["110"] = Diameter110 ?? "",
                ["125"] = Diameter125 ?? "",
                ["160"] = Diameter160 ?? "",
                ["195/80"] = Diameter19580 ?? "",
                ["235/80"] = Diameter23580 ?? ""
            };

            _artikelnummerData[SelectedProductType] = diameters;

            var filePath = Path.Combine(_networkPath, SettingsFileName);
            var json = JsonConvert.SerializeObject(_artikelnummerData, Formatting.Indented);
            File.WriteAllText(filePath, json);

            MessageBox.Show("Artikelnummers succesvol opgeslagen!", "Succes",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij opslaan: {ex.Message}", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void SavePrefabTemplateSettings(string projectNummer)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectNummer))
                throw new Exception("Projectnummer ontbreekt – kan PrefabManager.json niet opslaan.");

            var existing = PrefabManagerHelper.LoadPrefabManager(projectNummer);

            var prefabTemplates = new PrefabTemplateSettings
            {
                M524 = new TemplateSet { Plan = SelectedM524Plan, Maatvoering = SelectedM524Maatvoering, View3D = SelectedM5243D },
                M521 = new TemplateSet { Plan = SelectedM521Plan, Maatvoering = SelectedM521Maatvoering, View3D = SelectedM5213D },
                M570 = new TemplateSet { Plan = SelectedM570Plan, Maatvoering = SelectedM570Maatvoering, View3D = SelectedM5703D }
            };

            existing["PrefabTemplates"] = JToken.FromObject(prefabTemplates);

            PrefabManagerHelper.SavePrefabManager(projectNummer, existing);

            MessageBox.Show("Instellingen succesvol opgeslagen!",
                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij opslaan van PrefabManager.json:\n{ex.Message}",
                "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSettings()
    {
        try
        {
            var filePath = Path.Combine(_networkPath, SettingsFileName);

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                _artikelnummerData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
                                     ?? new Dictionary<string, Dictionary<string, string>>();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij laden van instellingen: {ex.Message}", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadAvailableViewTemplates()
    {
        try
        {
            var doc = RevitContext.Uidoc?.Document;
            if (doc == null) return;

            // Voeg een lege optie toe aan alle lijsten
            AvailableViewTemplates.Add("");
            AvailablePlanTemplates.Add("");
            Available3DTemplates.Add("");

            // Haal alle view templates op uit Revit
            var allViewTemplates = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .OrderBy(v => v.Name)
                .ToList();

            foreach (var view in allViewTemplates)
            {
                string templateName = view.Name;

                // Voeg toe aan algemene lijst
                AvailableViewTemplates.Add(templateName);

                // Splits op basis van view type
                if (view.ViewType == ViewType.ThreeD)
                {
                    Available3DTemplates.Add(templateName);
                }
                else if (view.ViewType == ViewType.FloorPlan || view.ViewType == ViewType.CeilingPlan)
                {
                    AvailablePlanTemplates.Add(templateName);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij laden van view templates: {ex.Message}", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static PrefabTemplateSettings LoadPrefabTemplateSettings(string projectNummer)
    {
        var fallbackSettings = new PrefabTemplateSettings
        {
            M524 = new TemplateSet { Plan = "P52_Riolering", Maatvoering = "P50_Lucht_Riolering_Maatvoering", View3D = "3D_P52_Riolering" },
            M521 = new TemplateSet { Plan = "P52_Riolering", Maatvoering = "", View3D = "3D_P52_Riolering" },
            M570 = new TemplateSet { Plan = "P57_Lucht", Maatvoering = "P50_Lucht_Riolering_Maatvoering", View3D = "3D_P57_Lucht" }
        };

        if (string.IsNullOrWhiteSpace(projectNummer))
            return fallbackSettings;

        try
        {
            var json = PrefabManagerHelper.LoadPrefabManager(projectNummer);
            var token = json["PrefabTemplates"];
            if (token == null)
                return fallbackSettings;

            return token.ToObject<PrefabTemplateSettings>() ?? fallbackSettings;
        }
        catch
        {
            return fallbackSettings;
        }
    }

    /// <summary>
    /// Statische methode voor het ophalen van artikelnummers (voor gebruik in andere classes)
    /// </summary>
    public static string GetArtikelnummer(string productType, string diameter)
    {
        try
        {
            var networkPath = @"F:\Revit\Plugin\Nijhof Panel\Data";
            var filePath = Path.Combine(networkPath, SettingsFileName);

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

                if (data != null && data.ContainsKey(productType) && data[productType].ContainsKey(diameter))
                {
                    return data[productType][diameter];
                }
            }
        }
        catch
        {
            // Return empty string bij fouten
        }

        return string.Empty;
    }
}

public class ProductTypeItem
{
    public string DisplayName { get; set; } = null!;
    public string Tag { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}