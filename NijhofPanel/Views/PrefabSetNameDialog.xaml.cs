namespace NijhofPanel.Views;

using System.Windows;

public partial class PrefabSetNameDialog : Window
{
    public string? ResultText { get; private set; }

    public PrefabSetNameDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdatePreview(); // preview pas updaten als de UI klaar is
    }

    private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        Placeholder.Visibility = string.IsNullOrWhiteSpace(ValueTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        // Wacht tot de UI-elementen bestaan
        if (TypeComboBox == null || ValueTextBox == null || PreviewText == null || Placeholder == null)
            return;

        string prefix = (TypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
        string value = ValueTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(prefix) && string.IsNullOrWhiteSpace(value))
        {
            PreviewText.Text = "— vul gegevens in —";
            return;
        }

        // Bij "Verd." komt de waarde eerst
        if (prefix.Equals("Verd.", StringComparison.OrdinalIgnoreCase))
        {
            PreviewText.Text = !string.IsNullOrWhiteSpace(value)
                ? $"{value} {prefix}"
                : $"{prefix}";
        }
        else
        {
            PreviewText.Text = !string.IsNullOrWhiteSpace(value)
                ? $"{prefix} {value}"
                : $"{prefix}";
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        string prefix = (TypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
        string value = ValueTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(value))
        {
            MessageBox.Show("Vul een waarde in.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Zelfde logica als preview
        if (prefix.Equals("Verd.", System.StringComparison.OrdinalIgnoreCase))
            ResultText = $"{value} {prefix}";
        else
            ResultText = $"{prefix} {value}";

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}