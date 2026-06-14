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
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(missingMonitors);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(text);

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
            : monitors.FirstOrDefault(monitor =>
                selectedMonitorKey is not null && MonitorKeys.Equals(monitor.MonitorKey, selectedMonitorKey))
                ?? monitors.FirstOrDefault();
}

internal sealed record MonitorRowsProjection
{
    public MonitorRowsProjection(
        double TopologyWidth,
        double TopologyHeight,
        MonitorRowViewModel? SelectedMonitor)
    {
        if (TopologyWidth <= 0 || !double.IsFinite(TopologyWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(TopologyWidth), TopologyWidth, "Topology width must be positive and finite.");
        }

        if (TopologyHeight <= 0 || !double.IsFinite(TopologyHeight))
        {
            throw new ArgumentOutOfRangeException(nameof(TopologyHeight), TopologyHeight, "Topology height must be positive and finite.");
        }

        this.TopologyWidth = TopologyWidth;
        this.TopologyHeight = TopologyHeight;
        this.SelectedMonitor = SelectedMonitor;
    }

    public double TopologyWidth { get; }

    public double TopologyHeight { get; }

    public MonitorRowViewModel? SelectedMonitor { get; }
}
