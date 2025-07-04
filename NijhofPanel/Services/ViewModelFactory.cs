namespace NijhofPanel.Services;

using ViewModels;

public class ViewModelFactory
{
    private readonly INavigationService _navigationService;
    private MainUserControlViewModel _mainViewModel;

    public ViewModelFactory()
    {
        _navigationService = new NavigationService();
        _mainViewModel = new MainUserControlViewModel(_navigationService);
        _navigationService.SetMainViewModel(_mainViewModel);
    }

    public MainUserControlViewModel CreateMainViewModel()
    {
        if (_mainViewModel == null)
        {
            _mainViewModel = new MainUserControlViewModel(_navigationService);
        }

        return _mainViewModel;
    }

    public INavigationService GetNavigationService()
    {
        return _navigationService;
    }
}