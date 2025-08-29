namespace NijhofPanel.Services;

using ViewModels;
using Services;

public class ViewModelFactory
{
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    private MainUserControlViewModel _mainViewModel;

    public ViewModelFactory()
    {
        _navigationService = new NavigationService();
        _windowService = new WindowService();
        _mainViewModel = new MainUserControlViewModel(_navigationService, _windowService);
        _navigationService.SetMainViewModel(_mainViewModel);
    }

    public MainUserControlViewModel CreateMainViewModel()
    {
        if (_mainViewModel == null)
        {
            _mainViewModel = new MainUserControlViewModel(_navigationService, _windowService);
        }

        return _mainViewModel;
    }

    public INavigationService GetNavigationService()
    {
        return _navigationService;
    }

    public IWindowService GetWindowService()
    {
        return _windowService;
    }
}