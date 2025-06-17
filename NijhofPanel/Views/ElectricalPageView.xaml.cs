using System.Windows.Controls;
using NijhofPanel.ViewModels;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace NijhofPanel.Views;

public partial class ElectricalPageView : Page
{
    public ElectricalPageView()
    {
        InitializeComponent();

        // Februik statische instance
        if (ElectricalPageViewModel.Instance != null)
        {
            DataContext = ElectricalPageViewModel.Instance;
        }
        else
        {
            // Fallback of foutmelding
            MessageBox.Show("ElectricalPageViewModel niet geïnitialiseerd.");
        }
    }
}