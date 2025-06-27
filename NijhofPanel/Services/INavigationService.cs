namespace NijhofPanel.Services;

using System.Windows;
using System.Windows.Controls;
using NijhofPanel.ViewModels;

public interface INavigationService
{
    void SetHost(Control host);
    void SetFrame(Frame frame);
    void Navigate(UIElement view);
    void NavigateTo<T>() where T : UIElement;
    void SetMainViewModel(MainUserControlViewModel viewModel);
}

public class NavigationService : INavigationService
{
    private MainUserControlViewModel _mainViewModel;
    private ContentControl? _host;
    private Frame _navigationFrame;

    public NavigationService()
    {
    }
    
    public void SetMainViewModel(MainUserControlViewModel viewModel)
    {
        _mainViewModel = viewModel;
    }

    public void SetHost(Control host)
    {
        _host = host as ContentControl;
    }

    public void SetFrame(Frame frame)
    {
        _navigationFrame = frame;
    }

    public void Navigate(UIElement view)
    {
        if (_host != null) _host.Content = view;
    }

    public void NavigateTo<T>() where T : UIElement
    {
        var view = (T)Activator.CreateInstance(typeof(T), _mainViewModel);
        Navigate(view);
    }
}
