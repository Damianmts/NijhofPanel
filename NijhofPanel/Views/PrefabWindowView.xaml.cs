using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NijhofPanel.ViewModels;
using NijhofPanel.Helpers;
using NijhofPanel.Commands;

namespace NijhofPanel.Views;

public partial class PrefabWindowView : Window
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