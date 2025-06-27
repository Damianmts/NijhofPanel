using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WixSharp;

namespace Installer;

public static class Generator
{
    /// <summary>
    ///     Generates Wix entities, features and directories for the installer.
    /// </summary>
    public static WixEntity[] GenerateWixEntities(IEnumerable<string> args, string[] excludedProjects = null)
    {
        var versionRegex = new Regex(@"\d+");
        var versionStorages = new Dictionary<string, List<WixEntity>>();

        var revitFeature = new Feature
        {
            Name = "Revit Add-in",
            Description = "Revit add-in installation files",
            Display = FeatureDisplay.expand
        };

        foreach (var directory in args)
        {
            var directoryInfo = new DirectoryInfo(directory);
            var fileVersion = versionRegex.Match(directoryInfo.Name).Value;
            var feature = new Feature
            {
                Name = fileVersion,
                Description = $"Install add-in for Revit {fileVersion}",
                ConfigurableDir = $"INSTALL{fileVersion}"
            };

            revitFeature.Add(feature);

            var files = new Files(feature, $@"{directory}\*.*",
                file => FilterEntities(file, excludedProjects));

            if (versionStorages.TryGetValue(fileVersion, out var storage))
                storage.Add(files);
            else
                versionStorages.Add(fileVersion, [files]);

            LogFeatureFiles(directory, fileVersion);
        }

        return versionStorages
            .Select(storage => new Dir(new Id($"INSTALL{storage.Key}"), storage.Key, storage.Value.ToArray()))
            .Cast<WixEntity>()
            .ToArray();
    }

    /// <summary>
    ///     Filter installer files and exclude from output. 
    /// </summary>
    private static bool FilterEntities(string file, string[] excludedProjects)
    {
        // Bestaande filter voor .pdb bestanden
        if (file.EndsWith(".pdb"))
            return false;

        // Als er geen uitgesloten projecten zijn, alleen .pdb filter toepassen
        if (excludedProjects == null || excludedProjects.Length == 0)
            return true;

        // Check of het bestand tot een uitgesloten project behoort
        return !excludedProjects.Any(excluded =>
            Path.GetFileName(file).StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///    Write a list of installer files.
    /// </summary>
    private static void LogFeatureFiles(string directory, string fileVersion)
    {
        var assemblies = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Installer files for version '{fileVersion}':");

        foreach (var assembly in assemblies.Where(f => FilterEntities(f, null))) Console.WriteLine($"'{assembly}'");
    }
}