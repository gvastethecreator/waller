using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

internal static class MonitorRowsSurface
{
    public static Visibility NoMonitorsVisibility(IReadOnlyCollection<MonitorRowViewModel> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        return VisibilityStates.When(monitors.Count == 0);
    }

    public static Visibility TopologyVisibility(IReadOnlyCollection<MonitorRowViewModel> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        return VisibilityStates.Unless(monitors.Count == 0);
    }

    public static Visibility MissingMonitorsVisibility(IReadOnlyCollection<MissingMonitorRowViewModel> missingMonitors)
    {
        ArgumentNullException.ThrowIfNull(missingMonitors);

        return VisibilityStates.Unless(missingMonitors.Count == 0);
    }
}
