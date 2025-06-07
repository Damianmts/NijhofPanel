using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace NijhofPanel.Services;

public class NavButtonService : ListBoxItem
{
    static NavButtonService()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavButtonService), new FrameworkPropertyMetadata(typeof(NavButtonService)));
    }

    public Uri Navlink
    {
        get { return (Uri)GetValue(NavLinkProperty); }
        set { SetValue(NavLinkProperty, value); }  
    }
    public static readonly DependencyProperty NavLinkProperty = DependencyProperty.Register("NavLink", typeof(Uri), typeof(NavButtonService), new PropertyMetadata(null));

    public Geometry Icon
    {
        get { return (Geometry)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(NavButtonService), new PropertyMetadata(null));
    
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
}