using Microsoft.WindowsAPICodePack.Shell;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace NijhofPanel.Services;

public static class ThumbnailHelper
{
    private static readonly SemaphoreSlim Semaphore = new(4);
    private static readonly ConcurrentDictionary<string, BitmapImage> Cache = new();
    private static readonly string CacheFolder = @"F:\Revit\Nijhof Tools\cache\";

    static ThumbnailHelper()
    {
        if (!Directory.Exists(CacheFolder))
            Directory.CreateDirectory(CacheFolder);
    }

    public static async Task<BitmapImage?> GetThumbnailAsync(string filePath)
    {
        if (Cache.TryGetValue(filePath, out var cached))
            return cached;

        var cacheFilePath = GetCacheFilePath(filePath);

        if (File.Exists(cacheFilePath) && IsCacheValid(filePath, cacheFilePath))
        {
            var image = new BitmapImage(new Uri(cacheFilePath));
            Cache.TryAdd(filePath, image);
            return image;
        }

        if (Path.GetExtension(filePath).ToLower() != ".rfa")
            return null;

        return await Task.Run(async () =>
        {
            await Semaphore.WaitAsync();
            try
            {
                var shellFile = ShellFile.FromFilePath(filePath);
                using var shellThumb = shellFile?.Thumbnail?.Bitmap;
                if (shellThumb == null) return null;

                using var resized = ResizeBitmap(shellThumb, 256, 256);
                using var mem = new MemoryStream();
                resized.Save(mem, ImageFormat.Png);
                File.WriteAllBytes(cacheFilePath, mem.ToArray());
                File.SetLastWriteTimeUtc(cacheFilePath, File.GetLastWriteTimeUtc(filePath));

                var result = ConvertToBitmapImage(resized);
                Cache.TryAdd(filePath, result);
                return result;
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }

    private static string GetCacheFilePath(string path)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(path));
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return Path.Combine(CacheFolder, hash + ".png");
    }

    private static bool IsCacheValid(string originalPath, string cachePath)
    {
        return File.Exists(cachePath) &&
               File.GetLastWriteTimeUtc(originalPath) <= File.GetLastWriteTimeUtc(cachePath);
    }

    private static Bitmap ResizeBitmap(Bitmap bitmap, int width, int height)
    {
        var result = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(result);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(bitmap, 0, 0, width, height);
        return result;
    }

    private static BitmapImage ConvertToBitmapImage(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }
}