namespace NijhofPanel.Core;

using Autodesk.Revit.UI;

public static class RevitContext
{
    public static UIApplication? UiApp { get; private set; }

    public static UIDocument? Uidoc => UiApp?.ActiveUIDocument;

    public static void SetUiApplication(UIApplication app)
    {
        UiApp = app;
    }
}