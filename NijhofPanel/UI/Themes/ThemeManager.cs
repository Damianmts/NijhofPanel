namespace NijhofPanel.UI.Themes;

using System.Windows;

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
        var sidebarIconKey = isDarkMode ? "SidebarIconColor_Dark" : "SidebarIconColor";

        if (element.Resources.Contains("CurrentBackgroundColor"))
            element.Resources["CurrentBackgroundColor"] = ThemeResources[backgroundKey];

        if (element.Resources.Contains("CurrentTextColor"))
            element.Resources["CurrentTextColor"] = ThemeResources[textKey];
        
        if (element.Resources.Contains("CurrentSidebarIconColor"))
            element.Resources["CurrentSidebarIconColor"] = ThemeResources[sidebarIconKey];
    }
}