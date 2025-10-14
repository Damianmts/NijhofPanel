namespace NijhofPanel.Helpers.Core;

using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using NijhofPanel.Core;

public static class RevitWindowHelper
{
    /// <summary>
    /// Stelt het Revit-hoofdvenster in als owner voor een WPF-venster.
    /// Werkt in Revit 2024, 2025 en 2026.
    /// </summary>
    public static void SetRevitOwner(Window window, UIApplication uiApp)
    {
        if (window == null || uiApp == null)
            return;

        try
        {
            IntPtr revitHandle = uiApp.MainWindowHandle;

            if (revitHandle != IntPtr.Zero)
            {
                var interop = new WindowInteropHelper(window)
                {
                    Owner = revitHandle
                };
            }
        }
        catch
        {
            // Revit-venster mogelijk nog niet geïnitialiseerd
        }
    }

    /// <summary>
    /// Haalt de actieve UIApplication op via de globale RevitContext.
    /// </summary>
    public static UIApplication? GetUIApplication()
    {
        return RevitContext.UiApp;
    }
}