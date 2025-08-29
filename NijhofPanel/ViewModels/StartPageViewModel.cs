namespace NijhofPanel.ViewModels;

public class StartPageViewModel : ObservableObject
{
    private readonly MainUserControlViewModel _mainVm;

    public StartPageViewModel(MainUserControlViewModel mainVm)
    {
        _mainVm = mainVm;
    }
}