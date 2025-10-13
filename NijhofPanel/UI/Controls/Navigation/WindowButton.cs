namespace NijhofPanel.UI.Controls.Navigation;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
/// Custom navigation button that triggers a command, typically used to open windows.
/// Revit-safe: does not directly manipulate window ownership.
/// </summary>
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
        DependencyProperty.Register(nameof(Navlink), typeof(string), typeof(WindowButton), new PropertyMetadata(null));

    public Geometry Icon
    {
        get => (Geometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(WindowButton), new PropertyMetadata(null));

    public bool IsWindowOpen
    {
        get => (bool)GetValue(IsWindowOpenProperty);
        set => SetValue(IsWindowOpenProperty, value);
    }

    public static readonly DependencyProperty IsWindowOpenProperty =
        DependencyProperty.Register(nameof(IsWindowOpen), typeof(bool), typeof(WindowButton),
            new PropertyMetadata(false));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(WindowButton),
            new PropertyMetadata(null));

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (Command == null)
            return;

        if (Command.CanExecute(this))
            Command.Execute(this); // laat ViewModel afhandelen
    }
}