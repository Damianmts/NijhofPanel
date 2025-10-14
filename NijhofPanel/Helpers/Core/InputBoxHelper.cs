namespace NijhofPanel.Helpers.Core;

using System.Linq;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Vervanger voor Microsoft.VisualBasic.Interaction.InputBox().
/// Werkt in zowel .NET Framework 4.8 als .NET 8.
/// </summary>
public static class InputBoxHelper
{
    public static string? Show(string prompt, string title = "Invoer vereist", string defaultValue = "")
    {
        var window = new Window
        {
            Title = title,
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Content = CreateContent(prompt, defaultValue)
        };

        // Pak de tekstbox en knoppen uit de content
        var content = (StackPanel)window.Content;
        var textBox = content.Children.OfType<TextBox>().First();
        var buttonsPanel = content.Children.OfType<StackPanel>().Last();
        var okButton = buttonsPanel.Children.OfType<Button>().First();
        var cancelButton = buttonsPanel.Children.OfType<Button>().Last();

        okButton.Click += (_, _) =>
        {
            window.DialogResult = true;
            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        // Toon venster modaal
        bool? result = window.ShowDialog();
        return result == true ? textBox.Text : null;
    }

    private static UIElement CreateContent(string prompt, string defaultValue)
    {
        var promptText = new TextBlock
        {
            Text = prompt,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var textBox = new TextBox
        {
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children =
            {
                new Button { Content = "OK", Width = 80, Margin = new Thickness(5, 0, 0, 0), IsDefault = true },
                new Button { Content = "Annuleren", Width = 80, Margin = new Thickness(5, 0, 0, 0), IsCancel = true }
            }
        };

        return new StackPanel
        {
            Margin = new Thickness(15),
            Children = { promptText, textBox, buttonsPanel }
        };
    }
}