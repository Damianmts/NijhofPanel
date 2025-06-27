using System.Reflection;
using System.Windows.Media.Imaging;

namespace NijhofPanel.UI;

public class ImageResource
{
    public BitmapImage? LoadImageFromResource(string resourcePath)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(resourcePath);

        if (stream == null) return null;

        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();

        return image;
    }
}