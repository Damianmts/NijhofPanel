namespace NijhofPanel.Services;

using Autodesk.Revit.UI;

public static class RevitContext
{
    public static UIApplication? UiApp { get; private set; }

    public static UIDocument? Uidoc => UiApp?.ActiveUIDocument;

    public static void SetUIApplication(UIApplication app)
    {
        UiApp = app;
    }
}