using Waller.Native.Core.Models;

namespace Waller.Native.Core.Presets;

public sealed class PresetMatcher
{
    public ActiveSession ApplyPreset(ActiveSession session, Preset preset)
    {
        var unmatchedAssignments = new List<PresetAssignment>();
        var assignmentIndex = BuildAssignmentIndex(preset.Assignments);

        var usedAssignmentKeys = MonitorKeys.CreateSet();
        var monitors = session.Monitors.Select(monitor =>
        {
            if (!assignmentIndex.ByMonitorKey.TryGetValue(monitor.Monitor.Identity.MonitorKey, out var assignment))
            {
                assignment = FindFallbackMatch(monitor.Monitor.Identity, assignmentIndex.UniqueAssignments, usedAssignmentKeys);
            }

            if (assignment is null)
            {
                return monitor;
            }

            usedAssignmentKeys.Add(assignment.SavedMonitor.MonitorKey);
            return monitor.WithPendingAssignment(
                assignment with { SavedMonitor = monitor.Monitor.Identity },
                hasUnsavedPresetChanges: false);
        }).ToList();

        foreach (var assignment in assignmentIndex.UniqueAssignments)
        {
            if (!usedAssignmentKeys.Contains(assignment.SavedMonitor.MonitorKey))
            {
                unmatchedAssignments.Add(assignment);
            }
        }

        return session with
        {
            Monitors = monitors,
            BasedOnPreset = preset.Identity,
            MissingAssignments = unmatchedAssignments,
            HasUnsavedPresetChanges = false,
        };
    }

    private static AssignmentIndex BuildAssignmentIndex(IReadOnlyList<PresetAssignment> assignments)
    {
        var uniqueAssignments = PresetAssignments.Normalize(assignments);
        var byMonitorKey = new Dictionary<string, PresetAssignment>(MonitorKeys.Comparer);

        foreach (var assignment in uniqueAssignments)
        {
            byMonitorKey.Add(assignment.SavedMonitor.MonitorKey, assignment);
        }

        return new AssignmentIndex(uniqueAssignments, byMonitorKey);
    }

    private static PresetAssignment? FindFallbackMatch(
        MonitorIdentity monitor,
        IReadOnlyList<PresetAssignment> assignments,
        ISet<string> usedAssignmentKeys)
    {
        return assignments
            .Where(assignment =>
                !usedAssignmentKeys.Contains(assignment.SavedMonitor.MonitorKey)
                && MonitorIdentityMatcher.IsFallbackCandidate(assignment.SavedMonitor, monitor))
            .OrderBy(assignment => MonitorIdentityMatcher.FallbackDistance(assignment.SavedMonitor, monitor))
            .ThenBy(assignment => assignment.SavedMonitor.DisplayIndex)
            .FirstOrDefault();
    }

    private sealed record AssignmentIndex(
        IReadOnlyList<PresetAssignment> UniqueAssignments,
        IReadOnlyDictionary<string, PresetAssignment> ByMonitorKey);
}
