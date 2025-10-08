namespace NijhofPanel.Services;

using System.IO;
using System.Linq;
using NijhofPanel.Helpers.Core;
using Autodesk.Revit.UI;

public class RevitLibraryActions : ILibraryActions
{
    private readonly RevitRequestHandler _handler;
    private readonly ExternalEvent _event;

    public RevitLibraryActions(RevitRequestHandler handler, ExternalEvent ev)
    {
        _handler = handler;
        _event = ev;
    }

    public void LoadFamily(string path)
    {
        _handler.Request = new RevitRequest(doc =>
        {
            var loader = new Commands.Tools.Com_LoadFamily();
            loader.Execute(doc, path);
        });
        _event.Raise();
    }

    public void PlaceFamily(string path)
    {
        _handler.Request = new RevitRequest(doc =>
        {
            try
            {
                var placer = new Commands.Tools.Com_PlaceFamily();
                placer.Execute(doc, path); // 🔹 hier wordt jouw debug-code aangeroepen
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Nijhof Library", $"❌ Fout tijdens plaatsen:\n{ex.Message}");
            }
        });

        _event.Raise();
    }

    private FamilySymbol? GetFamilySymbol(Document doc, string path)
    {
        var familyName = Path.GetFileNameWithoutExtension(path);
        return new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(f => f.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase));
    }
}