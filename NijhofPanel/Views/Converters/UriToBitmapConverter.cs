namespace NijhofPanel.Views.Converters;

using System;
using System.IO;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

[ValueConversion(typeof(Uri), typeof(BitmapImage))]
public class UriToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Uri uri)
            return null;

        try
        {
            // check of bestand bestaat — voorkomt binding errors
            if (!File.Exists(uri.LocalPath))
                return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;   // laadt het bestand meteen, niet lazy
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.DecodePixelWidth = 256;                   // consistent met je helper
            bmp.EndInit();
            bmp.Freeze();                                 // thread-safe voor UI-binding
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}