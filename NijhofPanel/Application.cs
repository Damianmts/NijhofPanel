using Nice3point.Revit.Toolkit.External;
using NijhofPanel.Commands;
using Autodesk.Revit.UI;
using NijhofPanel.Services;
using NijhofPanel.ViewModels;
using NijhofPanel.Views;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using JetBrains.Annotations;
using Syncfusion.Licensing;

namespace NijhofPanel;

/// <summary>
///     Application entry point
/// </summary>

// TODO - Add button to open DockablePane in a Window view. (Only one instance can exist. Panel or window)

[UsedImplicitly]
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        SyncfusionLicenseProvider.RegisterLicense
            ("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXpccHRWRWBfV010XUdWYUA=");
        
        System.Windows.Application.ResourceAssembly = typeof(Application).Assembly;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var assemblyPath = typeof(Application).Assembly.Location;
            return Assembly.LoadFrom(assemblyPath);
        };
        
        // Create 'Nijhof Tools' panel and buttons at 'Add-Ins'
        var ribbonPanel = GetOrCreateRibbonPanel();
        AddButtonToPanel(ribbonPanel);
        
        // Register DockablePane
        var viewModel = new MainUserControlViewModel();
        var mainView = new MainUserControlView(viewModel);
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
            throw new InvalidOperationException("Kon panel niet vinden/aanmaken: " + ex.Message, ex);
        }
    }

    private void AddButtonToPanel(RibbonPanel ribbonPanel)
    {
        string assemblyPath = typeof(Application).Assembly.Location;
        var iconService = new IconService();
        
        PushButtonData buttonData = new PushButtonData(
            "Open Panel",
            "Open Panel",
            assemblyPath,
            "NijhofPanel.Commands.Core.Com_OpenPanel"
        )
        {
            ToolTip = "Klik om een actie uit te voeren",
            LongDescription = "Een langere beschrijving van wat deze knop doet."
        };
        
        var icon = iconService.LoadImageFromResource("NijhofPanel.Resources.Icons.NijhofLogo_32x32.png");
        if (icon != null)
        {
            buttonData.LargeImage = icon;
            buttonData.Image = icon;
        }
        
        ribbonPanel.AddItem(buttonData);
        
        PushButtonData toggleButtonData = new PushButtonData(
            "Toggle Window",
            "Open als Venster",
            assemblyPath,
            "NijhofPanel.Commands.Core.Com_ToggleWindow"
        )
        {
            ToolTip = "Open het paneel als los venster",
            LongDescription = "Schakel tussen dockable panel en los venster"
        };
        
        if (icon != null)
        {
            toggleButtonData.LargeImage = icon;
            toggleButtonData.Image = icon;
        }
    
        ribbonPanel.AddItem(toggleButtonData);

    }
}