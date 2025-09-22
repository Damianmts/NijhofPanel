namespace NijhofPanel;

using System.Globalization;
using System.Reflection;
using System.Threading;
using Autodesk.Revit.UI;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using Services;
using ViewModels;
using Views;
using UI;
using Core;
using Providers;
using Helpers.Core;
using Helpers.Electrical;
using Commands.Tools;

/// <summary>
///     Entry point voor de Revit-plugin 'Nijhof Tools'
/// </summary>

// TODO - Fix all warnings in project :/

[UsedImplicitly]
public class RevitApplication : ExternalApplication
{
    
    public static RevitRequestHandler LibraryHandler { get; private set; }
    public static ExternalEvent LibraryEvent { get; private set; }
    public static Com_ConnectElement ConnectElementHandler { get; private set; } // <-- toegevoegd
    public static ExternalEvent ConnectElementEvent { get; private set; }        // <-- toegevoegd
    
    public override void OnStartup()
    {
        // WPF-resources en cultuurinstelling
        System.Windows.Application.ResourceAssembly = typeof(RevitApplication).Assembly;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        AppDomain.CurrentDomain.AssemblyResolve += (_, _) =>
            Assembly.LoadFrom(typeof(RevitApplication).Assembly.Location);

        // RevitContext instellen zodra een document opent
        Application.ControlledApplication.DocumentOpened += (_, args) =>
            RevitContext.SetUiApplication(new UIApplication(args.Document.Application));

        // ExternalEvent handlers
        var familyHandler = new FamilyPlacementHandler();
        var familyEvent = ExternalEvent.Create(familyHandler);
        var prefabHandler = new RevitRequestHandler();
        var prefabEvent = ExternalEvent.Create(prefabHandler);
        
        LibraryHandler = new RevitRequestHandler();
        LibraryEvent   = ExternalEvent.Create(LibraryHandler);
        ConnectElementHandler = new Com_ConnectElement();                  // <-- toegevoegd
        ConnectElementEvent   = ExternalEvent.Create(ConnectElementHandler); // <-- toegevoegd

        // Ribbon-buttons
        var ribbonPanel = GetOrCreateRibbonPanel();
        AddButtonToPanel(ribbonPanel);

        // Bouw sub-VM's
        var electricalVm = new ElectricalPageViewModel(familyHandler, familyEvent);
        var toolsVm = new ToolsPageViewModel();
        var prefabVm = new PrefabWindowViewModel(prefabHandler, prefabEvent);
        var libraryVm = new LibraryWindowViewModel(LibraryHandler, LibraryEvent);

        // Maak de hoofd-VM met de NavigationService
        var viewModelFactory = new ViewModelFactory();
        var navigationService = viewModelFactory.GetNavigationService();
        var windowService = new WindowService();
        var mainVm = new MainUserControlViewModel(navigationService, windowService)
        {
            ElectricalVm = electricalVm,
            ToolsVm = toolsVm,
            PrefabVm = prefabVm,
            LibraryVm = libraryVm
        };
        navigationService.SetMainViewModel(mainVm);

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
            "Open Venster",
            assemblyPath,
            "NijhofPanel.Commands.Core.Com_ToggleWindow")
        {
            ToolTip = "Open het paneel als losstaand venster",
            LongDescription = "Schakelt tussen dockable panel en los vensterweergave",
            LargeImage = icon,
            Image = icon
        };
        ribbonPanel.AddItem(toggleButtonData);
        
        ribbonPanel.AddSlideOut();
        var connectShortcutButton = new PushButtonData(
            "Aansluiten \nElementen",
            "Aansluiten \nElementen",
            assemblyPath,
            "NijhofPanel.Commands.Tools.Shortcuts.Cmd_ConnectElementShortcut")
        
        {
            ToolTip = "Verbind elementen",
            LongDescription = "Langere beschrijving",
            LargeImage = icon,
            Image = icon
        };
        ribbonPanel.AddItem(connectShortcutButton);
    }
}