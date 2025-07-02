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
    public ToolsPageView(MainUserControlViewModel? mainVm = null)
    {
        InitializeComponent();

        // Bepaal de viewmodel‐instance: geef prioriteit aan de geleverde mainVm, anders de singleton
        var vm = mainVm?.ToolsVm ?? ToolsPageViewModel.Instance;

        if (vm != null)
        {
            DataContext = vm;
        }
        else
        {
            MessageBox.Show("ToolsPageViewModel niet geïnitialiseerd.");
        }
    }
}