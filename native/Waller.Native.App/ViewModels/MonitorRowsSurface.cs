using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

internal static class MonitorRowsSurface
{
    public static Visibility NoMonitorsVisibility(IReadOnlyCollection<MonitorRowViewModel> monitors) =>
        VisibilityStates.When(monitors.Count == 0);

    public static Visibility TopologyVisibility(IReadOnlyCollection<MonitorRowViewModel> monitors) =>
        VisibilityStates.Unless(monitors.Count == 0);

    public static Visibility MissingMonitorsVisibility(IReadOnlyCollection<MissingMonitorRowViewModel> missingMonitors) =>
        VisibilityStates.Unless(missingMonitors.Count == 0);
}
