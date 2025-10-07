namespace NijhofPanel.Commands.Tools;

using System.IO;
using System.Windows;
using Autodesk.Revit.DB;

public class Com_LoadFamily
{
    /// <summary>
    /// Loopt binnen een ExternalEvent/RevitRequest en krijgt een live Document.
    /// </summary>
    public bool Execute(Document doc, string familyPath)
    {
        if (string.IsNullOrEmpty(familyPath))
        {
            MessageBox.Show("Geen familybestand geselecteerd.", "Waarschuwing",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!File.Exists(familyPath))
        {
            MessageBox.Show($"Het geselecteerde bestand bestaat niet: {familyPath}", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            using (var tx = new Transaction(doc, "Family inladen"))
            {
                tx.Start();

                bool loaded = doc.LoadFamily(familyPath, out var fam);
                tx.Commit();

                // TODO - Remove 'success' messages
                if (loaded && fam != null)
                {
                    MessageBox.Show($"Family '{fam.Name}' ingeladen!", "Succes",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show("Kon de family niet inladen.", "Fout",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij het inladen van de family: {ex.Message}", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}
