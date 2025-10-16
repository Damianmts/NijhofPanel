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
        if (DataContext is PrefabWindowViewModel viewModel)
        {
            viewModel.CollectAndSaveData();

            // Start live synchronisatie met Revit
            viewModel.StartRevitChangeListener();

            // Direct ook even een initiële status-update van de checkboxen
            viewModel.RefreshScheduleStatusFromRevit();
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (DataContext is PrefabWindowViewModel viewModel)
        {
            // Stop luisteren zodra het venster sluit
            viewModel.StopRevitChangeListener();
        }
    }
}