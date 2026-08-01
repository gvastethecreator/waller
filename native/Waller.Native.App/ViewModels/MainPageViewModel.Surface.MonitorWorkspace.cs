using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public Visibility NoMonitorsVisibility =>
        MonitorRowsSurface.NoMonitorsVisibility(Monitors);

    public Visibility TopologyVisibility =>
        MonitorRowsSurface.TopologyVisibility(Monitors);

    public Visibility MissingMonitorsVisibility =>
        MonitorRowsSurface.MissingMonitorsVisibility(MissingMonitors);
}
