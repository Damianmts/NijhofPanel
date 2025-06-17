using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NijhofPanel.ViewModels;
using NijhofPanel.Helpers;
using NijhofPanel.Commands;

namespace NijhofPanel.Views;

public partial class ToolsPageView : Page
{
    public ToolsPageView()
    {
        InitializeComponent();

        // Februik statische instance
        if (ToolsPageViewModel.Instance != null)
        {
            DataContext = ToolsPageViewModel.Instance;
        }
        else
        {
            // Fallback of foutmelding
            MessageBox.Show("ToolsPageViewModel niet geïnitialiseerd.");
        }
    }
}