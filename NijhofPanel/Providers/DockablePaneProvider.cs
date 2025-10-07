using Autodesk.Revit.UI;
using System.Windows.Controls;

namespace NijhofPanel.Providers;

public class DockablePaneProvider : IDockablePaneProvider
{
    private UserControl _control;

    public DockablePaneProvider(UserControl control)
    {
        _control = control;
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = _control;
        data.InitialState = new DockablePaneState()
        {
            DockPosition = DockPosition.Right
        };
    }
}