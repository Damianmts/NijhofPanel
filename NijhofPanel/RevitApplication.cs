using System.Globalization;
using System.Reflection;
using System.Threading;
using Autodesk.Revit.UI;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using NijhofPanel.Commands;
using NijhofPanel.Services;
using NijhofPanel.ViewModels;
using NijhofPanel.Views;
using NijhofPanel.Helpers;

namespace NijhofPanel;

/// <summary>
///     Entry point voor de Revit-plugin 'Nijhof Tools'
/// </summary>
[UsedImplicitly]
public class RevitApplication : ExternalApplication
{
    public override void OnStartup()
    {
        // Zorg dat WPF-resources juist geladen worden
        System.Windows.Application.ResourceAssembly = typeof(RevitApplication).Assembly;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        // Laad ontbrekende dependencies dynamisch indien nodig
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var assemblyPath = typeof(RevitApplication).Assembly.Location;
            return Assembly.LoadFrom(assemblyPath);
        };

        // Zet RevitContext zodra een document wordt geopend
        Application.ControlledApplication.DocumentOpened += (_, args) =>
        {
            var doc = args.Document;
            var uiApp = new UIApplication(doc.Application);
            RevitContext.SetUIApplication(uiApp);
        };

        // 🔧 ExternalEvent + handler aanmaken
        var handler = new FamilyPlacementHandler();
        var externalEvent = ExternalEvent.Create(handler);

        // 🎛 Ribbon aanmaken
        var ribbonPanel = GetOrCreateRibbonPanel();
        AddButtonToPanel(ribbonPanel);

        // 📌 DockablePane: view en viewmodel aanmaken en koppelen
        var electricalVm = new ElectricalPageViewModel(handler, externalEvent);
        var mainVm = new MainUserControlViewModel
        {
            ElectricalVm = electricalVm // <-- Zorg dat MainVM deze property heeft
        };

        var mainView = new MainUserControlView(mainVm);

        var dockablePaneId = new DockablePaneId(new Guid("e54d1236-371d-4b8b-9c93-30c9508f2fb9"));
        var dockablePaneService = new DockablePaneService(mainView);

        Application.RegisterDockablePane(dockablePaneId, "Nijhof Tools", dockablePaneService);
    }


    private RibbonPanel GetOrCreateRibbonPanel()
    {
        try
        {
            return Application.CreateRibbonPanel("Nijhof Tools");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Kon ribbonpaneel niet maken: " + ex.Message, ex);
        }
    }

    private void AddButtonToPanel(RibbonPanel ribbonPanel)
    {
        string assemblyPath = typeof(RevitApplication).Assembly.Location;
        var iconService = new IconService();

        var icon = iconService.LoadImageFromResource("NijhofPanel.Resources.Icons.NijhofLogo_32x32.png");

        var buttonData = new PushButtonData(
            "Open Panel",
            "Open Panel",
            assemblyPath,
            "NijhofPanel.Commands.Core.Com_OpenPanel")
        {
            ToolTip = "Klik om het zijpaneel te openen",
            LongDescription = "Opent het dockable panel met tools voor elektrische componenten",
            LargeImage = icon,
            Image = icon
        };

        ribbonPanel.AddItem(buttonData);

        var toggleButtonData = new PushButtonData(
            "Toggle Window",
            "Open als Venster",
            assemblyPath,
            "NijhofPanel.Commands.Core.Com_ToggleWindow")
        {
            ToolTip = "Open het paneel als losstaand venster",
            LongDescription = "Schakelt tussen dockable panel en los vensterweergave",
            LargeImage = icon,
            Image = icon
        };

        ribbonPanel.AddItem(toggleButtonData);
    }
}
