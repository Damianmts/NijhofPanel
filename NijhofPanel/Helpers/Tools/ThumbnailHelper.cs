namespace NijhofPanel.Helpers.Tools;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class ThumbnailHelper
{
    private static readonly string CacheFolder = @"F:\Revit\Nijhof Tools\cache\";

    static ThumbnailHelper()
    {
        if (!Directory.Exists(CacheFolder))
            Directory.CreateDirectory(CacheFolder);
    }

    /// <summary>
    /// Geeft een URI terug naar de PNG-thumbnail als deze in de cache bestaat.
    /// </summary>
    public static async Task<Uri?> GetThumbnailUriAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) ||
            !filePath.EndsWith(".rfa", StringComparison.OrdinalIgnoreCase))
            return null;

        var cacheFile = GetCacheFilePath(filePath);

        if (!File.Exists(cacheFile))
        {
            System.Diagnostics.Debug.WriteLine($"🔴 Geen thumbnail in cache: {filePath}");
            return null;
        }

        // check of cache nieuwer is dan family
        var valid = File.GetLastWriteTimeUtc(cacheFile) >= File.GetLastWriteTimeUtc(filePath);
        if (!valid)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Cache verouderd: {filePath}");
            return null;
        }

        // async return zodat DataGrid / WPF niet blokkeert
        await Task.Yield();
        return new Uri(cacheFile, UriKind.Absolute);
    }

    /// <summary>
    /// Maakt een pad voor het cachebestand op basis van een hash van de family-locatie.
    /// </summary>
    private static string GetCacheFilePath(string path)
    {
        using var md5 = MD5.Create();
        var hash = BitConverter
            .ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant())))
            .Replace("-", "")
            .ToLowerInvariant();

        return Path.Combine(CacheFolder, $"{hash}.png");
    }
}