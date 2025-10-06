namespace NijhofPanel.Commands.Electrical;

using System.Windows.Threading;
using Autodesk.Revit.UI;
using Views;

// Tagt de elementen van de opgegeven categorieën waar een Groepnummer is ingevuld.
// Inclusief batch-verwerking en een laadscherm voor grote aantallen elementen. Voorheen zat je 4 minuten naar een laad-icoon te staren.
// Ook wordt ervoor gezorgd dat die niet dubbel tagt.

// TODO - (Misschien) Nog weer de code toevoegen die de tag in het project laad wanneer dat nog niet zo is.

public class Com_TagGroepnummer : IExternalEventHandler
{
    private readonly Dictionary<BuiltInCategory, string> _categories;
    private ProgressWindowView? _progressWindow;

    public Com_TagGroepnummer()
    {
        _categories = new Dictionary<BuiltInCategory, string>
        {
            { BuiltInCategory.OST_FireAlarmDevices, "Brandmelders" },
            { BuiltInCategory.OST_LightingDevices, "Schakelaars" },
            { BuiltInCategory.OST_LightingFixtures, "Verlichting" },
            { BuiltInCategory.OST_ElectricalFixtures, "Elektra" }
        };
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = app.ActiveUIDocument.Document;

            var tagSymbol = GetTagFamily(doc, "Groep Tag");
            if (tagSymbol == null)
            {
                TaskDialog.Show("Fout", "De tag-familie 'Groep Tag' is niet geladen in het project.");
                return;
            }

            EnsureTagFamilyIsActive(doc, tagSymbol);

            var totalElements = _categories.Sum(category => GetFilteredElements(doc, category.Key, tagSymbol).Count);
            if (totalElements == 0)
            {
                TaskDialog.Show("Info", "Er zijn geen elementen gevonden om te taggen. (Alle elementen zijn al getagd.)");
                return;
            }

            InitializeComponents();

            var task = ProcessCategories(doc, tagSymbol);
            var frame = new DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.PushFrame(frame);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"Er is een fout opgetreden: {ex.Message}");
        }
    }

    public string GetName()
    {
        return "Com_TagGroepnummer";
    }

    private void InitializeComponents()
    {
        _progressWindow = new ProgressWindowView();
        _progressWindow.Show();
    }

    private async Task ProcessCategories(Document doc, FamilySymbol tagSymbol)
    {
        try
        {
            foreach (var category in _categories) await ProcessCategory(doc, category, tagSymbol);
        }
        finally
        {
            _progressWindow?.Dispatcher.Invoke(() => _progressWindow.Close());
        }
    }

    private async Task ProcessCategory(Document doc, KeyValuePair<BuiltInCategory, string> category,
        FamilySymbol tagSymbol)
    {
        UpdateProgressWindowStatus(category.Value);

        var elements = GetFilteredElements(doc, category.Key, tagSymbol);
        if (!elements.Any())
            return;

        var batches = CreateBatches(elements, CalculateBatchSize(elements.Count));
        await ProcessBatches(doc, batches, elements.Count, tagSymbol);
    }

    private void UpdateProgressWindowStatus(string categoryName)
    {
        _progressWindow?.Dispatcher.Invoke(() => _progressWindow.UpdateStatusText(categoryName));
    }

    private IList<Element> GetFilteredElements(Document doc, BuiltInCategory category, FamilySymbol tagSymbol)
    {
        var elementsWithGroup = new FilteredElementCollector(doc)
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .Where(el => !string.IsNullOrWhiteSpace(el.LookupParameter("Groep")?.AsString()))
            .ToList();

        var existingGroupTags = new FilteredElementCollector(doc, doc.ActiveView.Id)
            .OfClass(typeof(IndependentTag))
            .Cast<IndependentTag>()
            .Where(tag => tag.GetTypeId() == tagSymbol.Id)
            .ToList();

        return elementsWithGroup
            .Where(el => !existingGroupTags.Any(tag =>
                tag.GetTaggedReferences().Any(r => r.ElementId == el.Id)
            ))
            .ToList();
    }

    private int CalculateBatchSize(int totalElements)
    {
        return totalElements switch
        {
            <= 100 => 5,
            <= 250 => 10,
            <= 750 => 50,
            _ => 100
        };
    }

    private IEnumerable<List<Element>> CreateBatches(IList<Element> elements, int batchSize)
    {
        for (var i = 0; i < elements.Count; i += batchSize) yield return elements.Skip(i).Take(batchSize).ToList();
    }

    private async Task ProcessBatches(Document doc, IEnumerable<List<Element>> batches, int totalElements,
        FamilySymbol tagSymbol)
    {
        var processedElements = 0;
        var updateFrequency = Math.Max(1, totalElements / 50);

        foreach (var batch in batches)
        {
            await ExecuteBatchAsync(doc, batch, tagSymbol);
            processedElements += batch.Count;
            if (processedElements % updateFrequency == 0 || processedElements == totalElements)
            {
                UpdateProgress(processedElements, totalElements);
                _progressWindow?.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
                await Task.Delay(50);
            }
        }
    }

    private void UpdateProgress(int processedElements, int totalElements)
    {
        _progressWindow?.Dispatcher.Invoke(() =>
        {
            _progressWindow.UpdateProgress(processedElements * 100 / totalElements);
        });
    }

    private async Task ExecuteBatchAsync(Document doc, List<Element> batch, FamilySymbol tagSymbol)
    {
        using (var tx = new Transaction(doc, "Tag Elements"))
        {
            tx.Start();
            try
            {
                foreach (var element in batch) CreateTagForElement(doc, element, tagSymbol);

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.RollBack();
                TaskDialog.Show("Error", $"Fout in transactie: {ex.Message}");
            }
        }

        await Task.Yield();
    }

    private FamilySymbol? GetTagFamily(Document doc, string tagName)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(fam => fam.Family.Name == tagName);
    }

    private void EnsureTagFamilyIsActive(Document doc, FamilySymbol tagSymbol)
    {
        if (!tagSymbol.IsActive)
            using (var t = new Transaction(doc, "Activeer Tag-familie"))
            {
                t.Start();
                tagSymbol.Activate();
                doc.Regenerate();
                t.Commit();
            }
    }

    private void CreateTagForElement(Document doc, Element element, FamilySymbol tagSymbol)
    {
        if (element.Location is not LocationPoint locationPoint)
            throw new InvalidOperationException($"Element {element.Id} heeft geen LocationPoint");

        var tag = IndependentTag.Create(
            doc,
            tagSymbol.Id,
            doc.ActiveView.Id,
            new Reference(element),
            false,
            TagOrientation.Horizontal,
            locationPoint.Point
        );

        tag.LookupParameter("Parameter to display")?.Set("Groep");
    }

    public void Dispose()
    {
        _progressWindow?.Close();
    }
}