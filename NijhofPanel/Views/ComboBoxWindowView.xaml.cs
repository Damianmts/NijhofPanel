namespace NijhofPanel.Views;

using System.Windows;
using System.Collections.Generic;
using System.Linq;

public partial class ComboBoxWindowView : Window
{
    public object SelectedItem { get; private set; }

    public ComboBoxWindowView(string title, string message, IEnumerable<object> options)
    {
        InitializeComponent();

        Title = title;
        TextMessage.Text = message;

        ComboOptions.ItemsSource = options;
        ComboOptions.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        SelectedItem = ComboOptions.SelectedItem;
        DialogResult = true;
    }
}