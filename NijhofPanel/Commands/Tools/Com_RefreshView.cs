namespace NijhofPanel.Commands.Tools;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

[Transaction(TransactionMode.Manual)]
public class Com_RefreshView : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        UIDocument uiDoc = app.ActiveUIDocument;
        Document doc = uiDoc.Document;
        View activeView = doc.ActiveView;

        using (Transaction trans = new Transaction(doc, "Toggle Temporary View Template"))
        {
            try
            {
                trans.Start();

                // Sla de huidige View Template op
                ElementId originalTemplateId = activeView.ViewTemplateId;

                // Zet de View Template tijdelijk uit als er eentje actief is
                if (originalTemplateId != ElementId.InvalidElementId)
                {
                    activeView.ViewTemplateId = ElementId.InvalidElementId;
                }

                // Regenerate om de wijzigingen te laten doorvoeren
                doc.Regenerate();

                // Zet de oorspronkelijke View Template terug
                if (originalTemplateId != ElementId.InvalidElementId)
                {
                    activeView.ViewTemplateId = originalTemplateId;
                }

                trans.Commit();
            }
            catch (System.Exception ex)
            {
                TaskDialog.Show("Fout", ex.Message);
                trans.RollBack();
            }
        }
    }

    public string GetName()
    {
        return "Refresh View Handler";
    }
}