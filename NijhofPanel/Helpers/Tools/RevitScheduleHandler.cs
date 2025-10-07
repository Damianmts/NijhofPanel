namespace NijhofPanel.Helpers.Tools;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class RevitScheduleHandler
{
    private readonly Dictionary<string, string> _scheduleTemplates = new()
    {
        { "VWA", "Vwa Mat" },
        { "HWA", "Hwa Mat" },
        { "MV", "MV Mat" }
    };

    public void HandleMaterialListSchedule(Document doc, string setNumber, string discipline)
    {
        if (!_scheduleTemplates.TryGetValue(discipline, out var templateName))
        {
            TaskDialog.Show("Waarschuwing",
                $"Geen materiaallijst template gevonden voor discipline: {discipline}");
            return;
        }

        using (var trans = new Transaction(doc, "Kopieer materiaallijst schedule"))
        {
            trans.Start();

            var originalSchedule = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .FirstOrDefault(s => s.Name == templateName);

            if (originalSchedule != null)
            {
                // Schedule kopiëren
                var newScheduleId = originalSchedule.Duplicate(ViewDuplicateOption.Duplicate);
                var newSchedule = doc.GetElement(newScheduleId) as ViewSchedule;

                if (newSchedule != null)
                {
                    // Nieuwe naam geven
                    newSchedule.Name = $"{discipline} Mat Set {setNumber}";

                    // Zoek het Prefab Set veld
                    ScheduleField? prefabSetField = null;
                    var definition = newSchedule.Definition;

                    for (var i = 0; i < definition.GetFieldCount(); i++)
                    {
                        var field = definition.GetField(i);
                        if (field.GetName() == "Prefab Set")
                        {
                            prefabSetField = field;
                            break;
                        }
                    }

                    if (prefabSetField != null)
                    {
                        // Verwijder eventuele bestaande filters
                        IList<ScheduleFilter> existingFilters = definition.GetFilters();
                        for (var i = existingFilters.Count - 1; i >= 0; i--)
                            if (existingFilters[i].FieldId == prefabSetField.FieldId)
                                definition.RemoveFilter(i);

                        // Voeg nieuw filter toe
                        var filter = new ScheduleFilter(
                            prefabSetField.FieldId,
                            ScheduleFilterType.Equal,
                            setNumber);
                        definition.AddFilter(filter);
                    }
                    else
                    {
                        TaskDialog.Show("Fout",
                            "De parameter 'Prefab Set' kon niet worden gevonden in de schedule.");
                    }
                }
            }
            else
            {
                TaskDialog.Show("Fout",
                    $"Kon de template schedule '{templateName}' niet vinden.");
            }

            trans.Commit();
        }
    }
}