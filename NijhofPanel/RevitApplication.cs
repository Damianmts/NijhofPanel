namespace NijhofPanel;

using System.Globalization;
using System.Reflection;
using System.Threading;
using Autodesk.Revit.UI;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Commands;
using Services;
using ViewModels;
using Views;
using Helpers;
using UI;
using Providers;
using Core;

/// <summary>
///     Entry point voor de Revit-plugin 'Nijhof Tools'
/// </summary>
[UsedImplicitly]
public class RevitApplication : ExternalApplication
{
    public override void OnStartup()
    {
        // WPF-resources en cultuurinstelling
        System.Windows.Application.ResourceAssembly = typeof(RevitApplication).Assembly;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            Assembly.LoadFrom(typeof(RevitApplication).Assembly.Location);

        // RevitContext instellen zodra een document opent
        Application.ControlledApplication.DocumentOpened += (_, args) =>
            RevitContext.SetUiApplication(new UIApplication(args.Document.Application));

        // ExternalEvent handlers
        var familyHandler = new FamilyPlacementHandler();
        var familyEvent = ExternalEvent.Create(familyHandler);
        var prefabHandler = new RevitRequestHandler();
        var prefabEvent = ExternalEvent.Create(prefabHandler);

        // Ribbon-buttons
        var ribbonPanel = GetOrCreateRibbonPanel();
        AddButtonToPanel(ribbonPanel);

        // Bouw sub-VM's
        var electricalVm = new ElectricalPageViewModel(familyHandler, familyEvent);
        var toolsVm = new ToolsPageViewModel(familyEvent);
        var prefabVm = new PrefabWindowViewModel(prefabHandler, prefabEvent);

        // Maak de hoofd-VM met de NavigationService
        var viewModelFactory = new ViewModelFactory();
        var navigationService = viewModelFactory.GetNavigationService();
        var mainVm = new MainUserControlViewModel(navigationService)
        {
            ElectricalVm = electricalVm,
            ToolsVm = toolsVm,
            PrefabVm = prefabVm
        };

        // Maak de view en injecteer de VM
        var mainView = new MainUserControlView(mainVm);

        // Registreer de dockable pane
        var paneId = new DockablePaneId(new Guid("e54d1236-371d-4b8b-9c93-30c9508f2fb9"));
        var provider = new DockablePaneProvider(mainView);
        Application.RegisterDockablePane(paneId, "Nijhof Tools", provider);
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
        var assemblyPath = typeof(RevitApplication).Assembly.Location;
        var iconService = new ImageResource();

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