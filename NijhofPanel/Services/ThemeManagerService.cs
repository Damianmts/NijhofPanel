using System.Windows;

namespace NijhofPanel.Services;

public static class ThemeManagerService
{
    private static ResourceDictionary ThemeResources { get; } = new ResourceDictionary
    {
        Source = new Uri("/NijhofPanel;component/Resources/Themes/Thm_DarkMode.xaml", UriKind.RelativeOrAbsolute)
    };

    public static void UpdateTheme(bool isDarkMode, FrameworkElement element)
    {
        var backgroundKey = isDarkMode ? "BackgroundColor_Dark" : "BackgroundColor_Light";
        var textKey = isDarkMode ? "TextColor_Dark" : "TextColor_Light";

        if (element.Resources.Contains("CurrentBackgroundColor"))
            element.Resources["CurrentBackgroundColor"] = ThemeResources[backgroundKey];

        if (element.Resources.Contains("CurrentTextColor"))
            element.Resources["CurrentTextColor"] = ThemeResources[textKey];
    }
}