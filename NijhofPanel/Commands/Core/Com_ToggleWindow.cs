using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using NijhofPanel.Views;
using NijhofPanel.ViewModels;
using System;

namespace NijhofPanel.Commands.Core;

[Transaction(TransactionMode.Manual)]
public class Com_ToggleWindow : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIApplication uiApp = commandData.Application;

            // Maak een nieuwe MainUserControlView aan
            var mainView = new MainUserControlView(viewModel: null);

            // Toggle los venster via ViewModel
            var viewModel = new MainUserControlViewModel();
            viewModel.ToggleWindowMode(mainView, uiApp);

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}