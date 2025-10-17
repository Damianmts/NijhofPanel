namespace NijhofPanel.Helpers.Core;

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Views;

public class ComboBoxHelper
{
    
    // View template selectie
    public static View SelectViewTemplate(Document doc, string title = "Selecteer View Template")
    {
        var templates = new FilteredElementCollector(doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(v => v.IsTemplate && v.ViewType == ViewType.ThreeD)
            .OrderBy(v => v.Name)
            .ToList();

        if (!templates.Any())
        {
            Autodesk.Revit.UI.TaskDialog.Show("Geen templates", "Er zijn geen 3D view templates beschikbaar.");
            return null;
        }

        var dialog = new ComboBoxWindowView(
            title,
            "Kies een 3D view template:",
            templates.Select(v => (object)v.Name)
        );

        if (dialog.ShowDialog() == true)
        {
            string selectedName = dialog.SelectedItem?.ToString();
            return templates.FirstOrDefault(v => v.Name == selectedName);
        }

        return null;
    }

    // Algemene versie voor toekomstig gebruik
    public static T SelectItem<T>(IEnumerable<T> items, string title = "Selecteer een item", string message = "Kies uit de lijst:")
    {
        var list = items?.ToList() ?? new List<T>();
        if (!list.Any())
            return default;

        var dialog = new ComboBoxWindowView(title, message, list.Cast<object>());
        return dialog.ShowDialog() == true ? (T)dialog.SelectedItem : default;
    }
}