namespace NijhofPanel.Helpers.Core;

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class PrefabManagerHelper
{
    private const string BasePath = @"T:\Data";
    private const string FolderName = "zzz Nijhof Panel [NIET VERWIJDEREN!]";
    private const string JsonFileName = "PrefabManager.json";

    /// <summary>
    /// Berekent het volledige pad naar de PrefabManager.json op basis van projectnummer.
    /// Zorgt dat de map bestaat en verborgen is.
    /// </summary>
    public static string EnsurePrefabManagerPath(string projectNummer)
    {
        if (string.IsNullOrWhiteSpace(projectNummer))
            throw new ArgumentException("Projectnummer ontbreekt.");

        var invul1 = projectNummer.Length >= 2 ? projectNummer.Substring(0, 2) + "000" : "";
        var projectPath = Path.Combine(BasePath, invul1, projectNummer);
        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Projectmap niet gevonden: {projectPath}");

        var nijhofPanelPath = Path.Combine(projectPath, FolderName);

        // Maak aan als het niet bestaat
        if (!Directory.Exists(nijhofPanelPath))
            Directory.CreateDirectory(nijhofPanelPath);

        // Zet als verborgen
        var dirInfo = new DirectoryInfo(nijhofPanelPath);
        if ((dirInfo.Attributes & FileAttributes.Hidden) == 0)
            dirInfo.Attributes |= FileAttributes.Hidden;

        return Path.Combine(nijhofPanelPath, JsonFileName);
    }

    /// <summary>
    /// Leest bestaande JSON en retourneert een JObject (of leeg object bij fout).
    /// </summary>
    public static JObject LoadPrefabManager(string projectNummer)
    {
        try
        {
            var jsonBestand = EnsurePrefabManagerPath(projectNummer);
            if (!File.Exists(jsonBestand))
                return new JObject();

            var json = File.ReadAllText(jsonBestand);
            return JObject.Parse(json);
        }
        catch
        {
            return new JObject();
        }
    }

    /// <summary>
    /// Schrijft een JObject naar de PrefabManager.json.
    /// </summary>
    public static void SavePrefabManager(string projectNummer, JObject data)
    {
        var jsonBestand = EnsurePrefabManagerPath(projectNummer);
        var jsonText = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(jsonBestand, jsonText);
    }

    /// <summary>
    /// Hulpfunctie voor simpele objecten (bijv. anonymous types of classes).
    /// </summary>
    public static void SavePrefabManagerObject(string projectNummer, object data, string sectionName)
    {
        var jsonBestand = EnsurePrefabManagerPath(projectNummer);
        JObject root;

        // Bestaande JSON laden of nieuw object maken
        if (File.Exists(jsonBestand))
        {
            var json = File.ReadAllText(jsonBestand);
            try
            {
                root = JObject.Parse(json);
            }
            catch
            {
                root = new JObject();
            }
        }
        else
        {
            root = new JObject();
        }

        // Nieuwe sectie toevoegen of bestaande vervangen
        root[sectionName] = JObject.FromObject(data);

        // Wegschrijven
        var jsonText = JsonConvert.SerializeObject(root, Formatting.Indented);
        File.WriteAllText(jsonBestand, jsonText);
    }
}