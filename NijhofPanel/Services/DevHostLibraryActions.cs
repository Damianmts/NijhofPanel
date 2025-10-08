namespace NijhofPanel.Services;

using System.Windows;

public class DevHostLibraryActions : ILibraryActions
{
    public void LoadFamily(string path)
    {
        MessageBox.Show("DevHost: LoadFamily vereist Revit.", "Info");
    }

    public void PlaceFamily(string path)
    {
        MessageBox.Show($"DevHost: Plaatsen van '{System.IO.Path.GetFileName(path)}' vereist Revit.", "Info");
    }
}
