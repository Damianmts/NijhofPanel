namespace NijhofPanel.Helpers;

using Autodesk.Revit.UI;

/// <summary>
/// Hanteert inkomende RevitRequests wanneer ExternalEvent.Raise() wordt aangeroepen.
/// </summary>
public class RevitRequestHandler : IExternalEventHandler
{
    /// <summary>
    /// De laatst ingestelde request; wordt éénmalig gebruikt en daarna op null gezet.
    /// </summary>
    public RevitRequest? Request { get; set; }

    /// <summary>
    /// Deze methode voert de callback uit binnen een Revit transaction.
    /// </summary>
    public void Execute(UIApplication uiApp)
    {
        if (Request == null)
            return;

        // Haal het actieve document op
        var doc = uiApp.ActiveUIDocument?.Document;
        if (doc != null)
        {
            Request.Execute(doc);
            Request = null;
        }
    }

    /// <summary>
    /// Een leesbare naam voor het ExternalEvent.
    /// </summary>
    public string GetName()
    {
        return "RevitRequestHandler";
    }
}