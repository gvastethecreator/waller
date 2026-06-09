using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorRowSelection
{
    public static PresetAssignment? ApplySelection(
        IReadOnlyList<MonitorRowViewModel> monitors,
        MonitorRowViewModel? selectedMonitor)
    {
        foreach (var monitor in monitors)
        {
            monitor.IsSelected = ReferenceEquals(monitor, selectedMonitor);
        }

        return selectedMonitor?.Session.DesiredAssignment;
    }
}
