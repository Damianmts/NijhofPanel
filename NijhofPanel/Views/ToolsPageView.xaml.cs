using System.Windows.Controls;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class ToolsPageView : Page
{
    public ToolsPageView()
    {
        InitializeComponent();
        DataContext = new ToolsPageViewModel();
    }
}