using System.Windows;

namespace NijhofPanel.Views;

public partial class ProgressWindowView : Window
{
    public ProgressWindowView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the progress bar value.
    /// </summary>
    /// <param name="percentage">The progress percentage (0-100).</param>
    public void UpdateProgress(int percentage)
    {
        Dispatcher.Invoke(() => { progressBar.Value = Math.Max(0, Math.Min(percentage, 100)); });
    }

    /// <summary>
    /// Reset the progress bar to 0.
    /// </summary>
    public void ResetProgress()
    {
        Dispatcher.Invoke(() => { progressBar.Value = 0; });
    }

    /// <summary>
    /// Update the status text to show the current category being processed.
    /// </summary>
    /// <param name="category">The name of the current category being processed.</param>
    public void UpdateStatusText(string category)
    {
        Dispatcher.Invoke(() => { statusTextBlock.Text = $"{category}"; });
    }
}