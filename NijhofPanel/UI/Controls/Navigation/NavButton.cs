using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NijhofPanel.Services;

namespace NijhofPanel.UI.Controls.Navigation;

public class NavButton : ListBoxItem
{
    public static readonly DependencyProperty NavigationServiceProperty =
        DependencyProperty.Register(nameof(NavigationService), typeof(INavigationService), typeof(NavButton));
    
    public INavigationService NavigationService
    {
        get => (INavigationService)GetValue(NavigationServiceProperty);
        set => SetValue(NavigationServiceProperty, value);
    }


    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(NavButton), new PropertyMetadata(false));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
    
    public Type ViewType
    {
        get => (Type)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    public static readonly DependencyProperty ViewTypeProperty =
        DependencyProperty.Register("ViewType", typeof(Type), typeof(NavButton), new PropertyMetadata(null));

    public Geometry Icon
    {
        get => (Geometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(Geometry), typeof(NavButton), new PropertyMetadata(null));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(NavButton), new PropertyMetadata(null));
    
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (Command != null)
        {
            if (Command.CanExecute(this))
                Command.Execute(this);
        }
        else if (ViewType != null && NavigationService != null)
        {
            var method = typeof(INavigationService).GetMethod(nameof(INavigationService.NavigateTo))
                ?.MakeGenericMethod(ViewType);
            method?.Invoke(NavigationService, null);
        }
    }
}
