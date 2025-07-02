using System.Windows;

namespace NijhofPanel.UI.Themes;

public static class ThemeManager
{
    private static ResourceDictionary ThemeResources { get; } = new()
    {
        Source = new Uri("/NijhofPanel;component/UI/Themes/Base/CoreTheme.xaml", UriKind.RelativeOrAbsolute)
    };

    public static void UpdateTheme(bool isDarkMode, FrameworkElement element)
    {
        var backgroundKey = isDarkMode ? "BackgroundColor_Dark" : "BackgroundColor";
        var textKey = isDarkMode ? "TextColor_Dark" : "TextColor";

        if (element.Resources.Contains("CurrentBackgroundColor"))
            element.Resources["CurrentBackgroundColor"] = ThemeResources[backgroundKey];

        if (element.Resources.Contains("CurrentTextColor"))
            element.Resources["CurrentTextColor"] = ThemeResources[textKey];
    }
}