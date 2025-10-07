namespace NijhofPanel.ViewModels;

using System.Windows.Input;
using NijhofPanel.Helpers.Core;

public class UserPageViewModel : ObservableObject
{
    private readonly MainUserControlViewModel _mainViewModel;

    // Login command zonder MVVM toolkit
    private ICommand? _loginCommand;

    public ICommand LoginCommand =>
        _loginCommand
            ??= new RelayCommands.RelayCommand(_ => ExecuteLogin());

    public UserPageViewModel(MainUserControlViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    private void ExecuteLogin()
    {
        // Je login logica hier
        _mainViewModel.IsLoggedIn = true;

        // In plaats van een nieuwe view te maken, maak gebruik van het MainViewModel
        _mainViewModel.NavigateToStartPage(); // Deze methode moet je toevoegen aan MainUserControlViewModel
    }
}