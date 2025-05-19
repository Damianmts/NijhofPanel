using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace NijhofPanel.Services;

public class Srv_NavButton : ListBoxItem
{
    static Srv_NavButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Srv_NavButton), new FrameworkPropertyMetadata(typeof(Srv_NavButton)));
    }

    public Uri Navlink
    {
        get { return (Uri)GetValue(NavLinkProperty); }
        set { SetValue(NavLinkProperty, value); }  
    }
    public static readonly DependencyProperty NavLinkProperty = DependencyProperty.Register("NavLink", typeof(Uri), typeof(Srv_NavButton), new PropertyMetadata(null));

    public Geometry Icon
    {
        get { return (Geometry)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(Srv_NavButton), new PropertyMetadata(null));
}