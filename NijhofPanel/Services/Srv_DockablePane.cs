using Autodesk.Revit.UI;
using System.Windows.Controls;

namespace NijhofPanel.Services;

public class Srv_DockablePane : IDockablePaneProvider
{
    private UserControl _control;

    public Srv_DockablePane(UserControl control)
    {
        _control = control;
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = _control;
        data.InitialState = new DockablePaneState()
        {
            DockPosition = DockPosition.Right,
        };
    }
}