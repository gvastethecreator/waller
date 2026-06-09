using System.Collections.ObjectModel;
using Waller.Native.Core.Models;
using Waller.Native.Core.Topology;

namespace Waller.Native.App.ViewModels;

internal static class MonitorRowsProjector
{
    public static MonitorRowsProjection ReplaceRows(
        ObservableCollection<MonitorRowViewModel> monitors,
        ObservableCollection<MissingMonitorRowViewModel> missingMonitors,
        ActiveSession session,
        LocalizedText text,
        string? selectedMonitorKey,
        bool selectFirst)
    {
        var topology = MonitorTopologyLayout.Calculate(
            session.Monitors.Select(monitor => monitor.Monitor.Bounds).ToList());

        monitors.Clear();
        foreach (var monitor in session.Monitors)
        {
            var tile = topology.TileFor(monitor.Monitor.Bounds);
            monitors.Add(new MonitorRowViewModel(
                monitor,
                text,
                tile.Left,
                tile.Top,
                tile.Width,
                tile.Height));
        }

        missingMonitors.Clear();
        foreach (var assignment in session.MissingAssignments)
        {
            missingMonitors.Add(new MissingMonitorRowViewModel(assignment, text));
        }

        return new MonitorRowsProjection(
            topology.SurfaceWidth,
            topology.SurfaceHeight,
            SelectMonitor(monitors, selectedMonitorKey, selectFirst));
    }

    private static MonitorRowViewModel? SelectMonitor(
        IReadOnlyList<MonitorRowViewModel> monitors,
        string? selectedMonitorKey,
        bool selectFirst) =>
        selectFirst
            ? monitors.FirstOrDefault()
            : monitors.FirstOrDefault(monitor => monitor.MonitorKey == selectedMonitorKey)
                ?? monitors.FirstOrDefault();
}

internal sealed record MonitorRowsProjection(
    double TopologyWidth,
    double TopologyHeight,
    MonitorRowViewModel? SelectedMonitor);
