using System.Windows;

namespace NijhofPanel.Helpers.Electrical;

public class ElectricalProperties
{
    public static readonly DependencyProperty ComponentTypeProperty =
        DependencyProperty.RegisterAttached(
            "ComponentType",
            typeof(string),
            typeof(ElectricalProperties),
            new PropertyMetadata(null));

    public static void SetComponentType(UIElement element, string value)
    {
        element.SetValue(ComponentTypeProperty, value);
    }

    public static string GetComponentType(UIElement element)
    {
        return (string)element.GetValue(ComponentTypeProperty);
    }
}