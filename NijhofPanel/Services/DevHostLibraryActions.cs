namespace NijhofPanel.Services;

using System.Windows;

public class DevHostLibraryActions : ILibraryActions
{
    public void LoadFamily(string path)
    {
        MessageBox.Show("DevHost: LoadFamily vereist Revit.", "Info");
    }

    public void PlaceFamily()
    {
        MessageBox.Show("DevHost: PlaceFamily vereist Revit.", "Info");
    }
}
