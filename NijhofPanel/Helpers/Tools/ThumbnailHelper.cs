namespace NijhofPanel.Helpers.Tools;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

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
            var image = LoadBitmapImage(cacheFilePath);
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
                var bitmap = await GetThumbnailFromShell(filePath, 256);
                if (bitmap == null) return null;

                // Opslaan naar cache
                SaveBitmapToFile(bitmap, cacheFilePath);
                File.SetLastWriteTimeUtc(cacheFilePath, File.GetLastWriteTimeUtc(filePath));

                Cache.TryAdd(filePath, bitmap);
                return bitmap;
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }

    private static async Task<BitmapImage?> GetThumbnailFromShell(string filePath, int size)
    {
        return await Task.Run(() =>
        {
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                System.Diagnostics.Debug.WriteLine($"  🔍 GetThumbnailFromShell start: {Path.GetFileName(filePath)}");

                // Verkrijg IShellItem
                var guid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"); // IID_IShellItem
                NativeMethods.SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref guid, out var shellItem);

                if (shellItem == null)
                {
                    System.Diagnostics.Debug.WriteLine($"  ❌ shellItem is null");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"  ✓ ShellItem verkregen");

                // Verkrijg thumbnail via IShellItemImageFactory
                var imageFactory = (IShellItemImageFactory)shellItem;
                var pixelSize = new SIZE { cx = size, cy = size };

                var hr = imageFactory.GetImage(pixelSize, SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                System.Diagnostics.Debug.WriteLine($"  GetImage HRESULT: 0x{hr:X8}");

                if (hBitmap == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine($"  ❌ hBitmap is IntPtr.Zero");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"  ✓ HBITMAP verkregen: {hBitmap}");

                // Converteer naar BitmapImage
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                System.Diagnostics.Debug.WriteLine(
                    $"  ✓ BitmapSource gemaakt: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight}");

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                stream.Position = 0;

                System.Diagnostics.Debug.WriteLine($"  ✓ Stream size: {stream.Length} bytes");

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                System.Diagnostics.Debug.WriteLine(
                    $"  ✅ BitmapImage gemaakt: {bitmap.PixelWidth}x{bitmap.PixelHeight}");

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  💥 Exception in GetThumbnailFromShell: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"     {ex.StackTrace}");
                return null;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(hBitmap);
                    System.Diagnostics.Debug.WriteLine($"  🗑️ HBITMAP deleted");
                }
            }
        });
    }

    private static BitmapImage LoadBitmapImage(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static void SaveBitmapToFile(BitmapImage bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.Create);
        encoder.Save(stream);
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

    #region Native Methods

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int cx;
        public int cy;
    }

    [Flags]
    private enum SIIGBF
    {
        SIIGBF_RESIZETOFIT = 0x00,
        SIIGBF_BIGGERSIZEOK = 0x01,
        SIIGBF_MEMORYONLY = 0x02,
        SIIGBF_ICONONLY = 0x04,
        SIIGBF_THUMBNAILONLY = 0x08,
        SIIGBF_INCACHEONLY = 0x10,
    }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
    }

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            ref Guid riid,
            [Out] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)]
            out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
    }

    #endregion
}