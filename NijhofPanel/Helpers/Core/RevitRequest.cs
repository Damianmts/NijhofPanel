namespace NijhofPanel.Helpers.Core;

using System;
using Autodesk.Revit.DB;

/// <summary>
/// Wrapt een callback die een Revit Document krijgt.
/// </summary>
public class RevitRequest
{
    private readonly Action<Document> _callback;

    public RevitRequest(Action<Document> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Wordt door de ExternalEvent-handler aangeroepen binnen de Revit transaction.
    /// </summary>
    internal void Execute(Document doc)
    {
        _callback(doc);
    }
}