using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NijhofPanel.UI.Controls.Navigation;

public class WindowButton : ListBoxItem
{
    static WindowButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowButton),
            new FrameworkPropertyMetadata(typeof(WindowButton)));
    }

    public string Navlink
    {
        get => (string)GetValue(NavlinkProperty);
        set => SetValue(NavlinkProperty, value);
    }

    public static readonly DependencyProperty NavlinkProperty =
        DependencyProperty.Register("NavLink", typeof(string), typeof(WindowButton), new PropertyMetadata(null));

    public Geometry Icon
    {
        get => (Geometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(Geometry), typeof(WindowButton), new PropertyMetadata(null));

    public bool IsWindowOpen
    {
        get => (bool)GetValue(IsWindowOpenProperty);
        set => SetValue(IsWindowOpenProperty, value);
    }

    public static readonly DependencyProperty IsWindowOpenProperty =
        DependencyProperty.Register("IsWindowOpen", typeof(bool), typeof(WindowButton),
            new PropertyMetadata(false));
}