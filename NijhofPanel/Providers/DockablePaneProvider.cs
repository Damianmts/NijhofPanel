using Autodesk.Revit.UI;


namespace NijhofPanel.Providers;

public class DockablePaneProvider : IDockablePaneProvider
{
    private System.Windows.Controls.UserControl _control;

    public DockablePaneProvider(System.Windows.Controls.UserControl control)
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