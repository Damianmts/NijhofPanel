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
        DataContext = new ElectricalPageViewModel();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            if (button.FindResource("SelectiePopup") is Popup popup)
            {
                popup.IsOpen = true;
            }
        }
    }
}