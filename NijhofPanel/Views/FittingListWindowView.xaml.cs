namespace NijhofPanel.Views;

using ViewModels;
using Autodesk.Revit.UI;
using NijhofPanel.Helpers.Core;

public partial class FittingListWindowView
{
    public FittingListWindowView(Document doc, RevitRequestHandler handler, ExternalEvent externalEvent)
    {
        InitializeComponent();
        DataContext = new FittingListWindowViewModel(doc, this, handler, externalEvent);
    }
}