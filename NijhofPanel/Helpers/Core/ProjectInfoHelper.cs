namespace NijhofPanel.Helpers.Core;

using Autodesk.Revit.DB;
using System.Linq;

public class ProjectInfoHelper
{
    /// <summary>
    /// Haalt het projectnummer op uit de actieve Revit-omgeving.
    /// </summary>
    public static string GetProjectNummer(Document? doc)
    {
        if (doc == null) return string.Empty;

        var projectInfo = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectInformation)
            .FirstElement();

        // Eerst proberen de custom parameter "Project Nummer"
        var projectNummer = projectInfo?.LookupParameter("Project Nummer")?.AsString();

        // Fallback: standaard Revit-parameter
        if (string.IsNullOrWhiteSpace(projectNummer))
            projectNummer = doc.ProjectInformation?.Number;

        return projectNummer ?? string.Empty;
    }
}