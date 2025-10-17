namespace NijhofPanel.Helpers.Core;

using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;

/// <summary>
/// Houdt Revit DocumentChanged in de gaten en roept een .NET event aan
/// voor de ViewModel (veilig buiten de API-context).
/// </summary>
public class ChangeWatcher
{
    public static event Action? ScheduleChanged;

    private static bool _isRegistered;

    public static void Register(UIApplication uiApp)
    {
        if (_isRegistered) return;
        uiApp.Application.DocumentChanged += OnDocumentChanged;
        _isRegistered = true;
    }

    public static void Unregister(UIApplication uiApp)
    {
        if (!_isRegistered) return;
        uiApp.Application.DocumentChanged -= OnDocumentChanged;
        _isRegistered = false;
    }

    private static void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
    {
        // Alleen reageren als er iets met schedules gebeurde
        var doc = e.GetDocument();
        if (doc == null) return;

        var changedIds = e.GetAddedElementIds();
        var deletedIds = e.GetDeletedElementIds();

        if (changedIds.Count == 0 && deletedIds.Count == 0)
            return;

        // Fire event richting ViewModel
        ScheduleChanged?.Invoke();
    }
}