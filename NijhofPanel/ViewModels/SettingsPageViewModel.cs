namespace NijhofPanel.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

public class SettingsPageViewModel : ObservableObject
{
    private const string SettingsFileName = "ArtikelnummerSettings.json";
    
    private string _networkPath;
    private Dictionary<string, Dictionary<string, string>> _artikelnummerData;
    
    private string _selectedProductType;
    private string _diameter40;
    private string _diameter50;
    private string _diameter75;
    private string _diameter80;
    private string _diameter90;
    private string _diameter110;
    private string _diameter125;
    private string _diameter160;
    private string _diameter19580;
    private string _diameter23580;

    public ObservableCollection<ProductTypeItem> ProductTypes { get; }
    public RelayCommand SaveCommand { get; }

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
    public string Diameter40
    {
        get => _diameter40;
        set => SetProperty(ref _diameter40, value);
    }

    public string Diameter50
    {
        get => _diameter50;
        set => SetProperty(ref _diameter50, value);
    }

    public string Diameter75
    {
        get => _diameter75;
        set => SetProperty(ref _diameter75, value);
    }

    public string Diameter80
    {
        get => _diameter80;
        set => SetProperty(ref _diameter80, value);
    }

    public string Diameter90
    {
        get => _diameter90;
        set => SetProperty(ref _diameter90, value);
    }

    public string Diameter110
    {
        get => _diameter110;
        set => SetProperty(ref _diameter110, value);
    }

    public string Diameter125
    {
        get => _diameter125;
        set => SetProperty(ref _diameter125, value);
    }

    public string Diameter160
    {
        get => _diameter160;
        set => SetProperty(ref _diameter160, value);
    }

    public string Diameter19580
    {
        get => _diameter19580;
        set => SetProperty(ref _diameter19580, value);
    }

    public string Diameter23580
    {
        get => _diameter23580;
        set => SetProperty(ref _diameter23580, value);
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
        
        InitializeArtikelnummerSettings();

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
    public string DisplayName { get; set; }
    public string Tag { get; set; }
    public bool IsEnabled { get; set; } = true;
}