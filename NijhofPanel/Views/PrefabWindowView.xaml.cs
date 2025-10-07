namespace NijhofPanel.Views;

using System.Windows;
using ViewModels;

public partial class PrefabWindowView
{
    public PrefabWindowView(PrefabWindowViewModel viewModel)
    {
        InitializeComponent();

        // Februik statische instance
        if (viewModel != null)
            DataContext = viewModel;
        else
            // Fallback of foutmelding
            MessageBox.Show("PrefabWindowViewModel niet geïnitialiseerd.");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrefabWindowViewModel viewModel) viewModel.CollectAndSaveData();
    }
}