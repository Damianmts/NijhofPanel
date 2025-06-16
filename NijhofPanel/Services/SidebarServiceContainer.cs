using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace NijhofPanel.Services;

public class NavButtonService : ListBoxItem
{
    private const string BasePath = "pack://application:,,,/NijhofPanel;component/Views/";

    static NavButtonService()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavButtonService),
            new FrameworkPropertyMetadata(typeof(NavButtonService)));
    }

    public Uri Navlink
    {
        get { return (Uri)GetValue(NavLinkProperty); }
        set { SetValue(NavLinkProperty, value); }
    }

    public static readonly DependencyProperty NavLinkProperty =
        DependencyProperty.Register("Navlink", typeof(Uri), typeof(NavButtonService), new PropertyMetadata(null));

    public Geometry Icon
    {
        get { return (Geometry)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(Geometry), typeof(NavButtonService), new PropertyMetadata(null));

    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(NavButtonService), new PropertyMetadata(null));

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (Command != null && Command.CanExecute(null))
        {
            Command.Execute(null);
        }
    }

    public bool IsActive
    {
        get { return (bool)GetValue(IsActiveProperty); }
        set { SetValue(IsActiveProperty, value); }
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register("IsActive", typeof(bool), typeof(NavButtonService), new PropertyMetadata(false));
}

public class WindowButtonService : ListBoxItem
{
    static WindowButtonService()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowButtonService),
            new FrameworkPropertyMetadata(typeof(WindowButtonService)));
    }

    public string Navlink
    {
        get { return (string)GetValue(NavlinkProperty); }
        set { SetValue(NavlinkProperty, value); }
    }

    public static readonly DependencyProperty NavlinkProperty =
        DependencyProperty.Register("NavLink", typeof(string), typeof(WindowButtonService), new PropertyMetadata(null));

    public Geometry Icon
    {
        get { return (Geometry)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(Geometry), typeof(WindowButtonService), new PropertyMetadata(null));

    public bool IsWindowOpen
    {
        get { return (bool)GetValue(IsWindowOpenProperty); }
        set { SetValue(IsWindowOpenProperty, value); }
    }

    public static readonly DependencyProperty IsWindowOpenProperty =
        DependencyProperty.Register("IsWindowOpen", typeof(bool), typeof(WindowButtonService),
            new PropertyMetadata(false));
}