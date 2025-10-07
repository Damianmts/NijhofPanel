namespace NijhofPanel.Services;

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

    public void PlaceFamily()
    {
        var cmd = new Commands.Tools.Com_PlaceFamily();
        cmd.Execute();
    }
}
