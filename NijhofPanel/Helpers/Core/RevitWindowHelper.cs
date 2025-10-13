namespace NijhofPanel.Helpers.Core;

using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Windows;

public class RevitWindowHelper
{
    /// <summary>
    /// Probeert het Revit-hoofdvenster als owner te koppelen aan een WPF-window.
    /// Veilig voor Revit 2024, 2025 en 2026.
    /// </summary>
    public static void SetRevitOwner(Window window)
    {
        if (window == null)
            return;

        try
        {
            var revitHandle = ComponentManager.ApplicationWindow;
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
            // Negeer als Revit venster nog niet volledig geïnitialiseerd is
        }
    }
}