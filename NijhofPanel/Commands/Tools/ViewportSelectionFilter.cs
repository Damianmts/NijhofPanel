namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

/// <summary>
/// Selection filter voor viewports
/// </summary>
public class ViewportSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is Viewport;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}