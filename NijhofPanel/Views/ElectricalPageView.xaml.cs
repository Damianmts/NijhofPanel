using System.Windows.Controls;
using NijhofPanel.ViewModels;

namespace NijhofPanel.Views;

public partial class ElectricalPageView : Page
{
    public ElectricalPageView()
    {
        InitializeComponent();
        DataContext = new ElectricalPageViewModel();
    }
}