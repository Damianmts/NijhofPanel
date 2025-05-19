using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using NijhofPanel.Views;

namespace NijhofPanel.Services;

public class Srv_WindowButton : ListBoxItem
{
    static Srv_WindowButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Srv_WindowButton),
            new FrameworkPropertyMetadata(typeof(Srv_WindowButton)));
    }

    public string Navlink
    {
        get { return (string)GetValue(NavlinkProperty); }
        set { SetValue(NavlinkProperty, value); }
    }

    public static readonly DependencyProperty NavlinkProperty =
        DependencyProperty.Register("Navlink", typeof(string), typeof(Srv_WindowButton), new PropertyMetadata(null));

    public Geometry Icon
    {
        get { return (Geometry)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(Geometry), typeof(Srv_WindowButton), new PropertyMetadata(null));
}