namespace NijhofPanel.Views;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NijhofPanel.Helpers.Core;
using NijhofPanel.ViewModels;

public partial class SawListWindowView
{
    public SawListWindowView(Document doc, RevitRequestHandler handler, ExternalEvent externalEvent)
    {
        InitializeComponent();
        DataContext = new SawListWindowViewModel(doc, this, handler, externalEvent);
    }
}