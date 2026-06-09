using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public sealed class ActiveSessionEditor
{
    public ActiveSession UpdateAssignment(
        ActiveSession session,
        string monitorKey,
        WallpaperSource source,
        WallpaperPlacement placement)
    {
        var normalizedPlacement = placement.NormalizeOffsets();
        var updated = false;
        var monitors = session.Monitors.Select(monitor =>
        {
            if (!MonitorKeys.Equals(monitor.Monitor.Identity.MonitorKey, monitorKey))
            {
                return monitor;
            }

            if (monitor.DesiredAssignment.Source == source
                && monitor.DesiredAssignment.Placement == normalizedPlacement)
            {
                return monitor;
            }

            updated = true;
            var nextAssignment = monitor.DesiredAssignment with
            {
                Source = source,
                Placement = normalizedPlacement,
            };

            return monitor.WithPendingAssignment(nextAssignment, hasUnsavedPresetChanges: true);
        }).ToList();

        if (!updated)
        {
            return session;
        }

        return session with
        {
            Monitors = monitors,
            HasUnsavedPresetChanges = true,
        };
    }

    public ActiveSession RemoveMissingAssignment(ActiveSession session, string monitorKey)
    {
        var missingAssignments = RemoveMissingAssignments(session.MissingAssignments, monitorKey);

        if (missingAssignments.Count == session.MissingAssignments.Count)
        {
            return session;
        }

        return session with
        {
            MissingAssignments = missingAssignments,
            HasUnsavedPresetChanges = true,
        };
    }

    public ActiveSession ReassignMissingAssignment(
        ActiveSession session,
        string missingMonitorKey,
        string targetMonitorKey)
    {
        var assignment = session.MissingAssignments.FirstOrDefault(assignment =>
            MonitorKeys.Equals(assignment.SavedMonitor.MonitorKey, missingMonitorKey));
        if (assignment is null)
        {
            return session;
        }

        var targetFound = false;
        var monitors = session.Monitors.Select(monitor =>
        {
            if (!MonitorKeys.Equals(monitor.Monitor.Identity.MonitorKey, targetMonitorKey))
            {
                return monitor;
            }

            targetFound = true;
            return monitor.WithPendingAssignment(
                ReassignToMonitor(assignment, monitor.Monitor.Identity),
                hasUnsavedPresetChanges: true);
        }).ToList();

        if (!targetFound)
        {
            return session;
        }

        return session with
        {
            Monitors = monitors,
            MissingAssignments = RemoveMissingAssignments(session.MissingAssignments, missingMonitorKey),
            HasUnsavedPresetChanges = true,
        };
    }

    private static PresetAssignment ReassignToMonitor(PresetAssignment assignment, MonitorIdentity monitor)
    {
        return PresetAssignments.Normalize(assignment with { SavedMonitor = monitor });
    }

    private static IReadOnlyList<PresetAssignment> RemoveMissingAssignments(
        IReadOnlyList<PresetAssignment> assignments,
        string monitorKey)
    {
        return assignments
            .Where(assignment => !MonitorKeys.Equals(assignment.SavedMonitor.MonitorKey, monitorKey))
            .ToList();
    }
}
