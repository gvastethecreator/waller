using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorRowSelection
{
    public static PresetAssignment? ApplySelection(
        IReadOnlyList<MonitorRowViewModel> monitors,
        MonitorRowViewModel? selectedMonitor)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        foreach (var monitor in monitors)
        {
            (monitor ?? throw new ArgumentException(
                "Monitor row selection cannot include null items.",
                nameof(monitors))).IsSelected = ReferenceEquals(monitor, selectedMonitor);
        }

        return selectedMonitor?.Session.DesiredAssignment;
    }
}
