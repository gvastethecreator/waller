using Waller.Native.Core.Models;

namespace Waller.Native.Core.Presets;

public sealed class PresetMatcher
{
    public ActiveSession ApplyPreset(ActiveSession session, Preset preset)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(preset);

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
            if (!MonitorKeys.Contains(usedAssignmentKeys, assignment.SavedMonitor.MonitorKey))
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
        IReadOnlySet<string> usedAssignmentKeys)
    {
        return assignments
            .Where(assignment =>
                !MonitorKeys.Contains(usedAssignmentKeys, assignment.SavedMonitor.MonitorKey)
                && MonitorIdentityMatcher.IsFallbackCandidate(assignment.SavedMonitor, monitor))
            .OrderBy(assignment => MonitorIdentityMatcher.FallbackDistance(assignment.SavedMonitor, monitor))
            .ThenBy(assignment => assignment.SavedMonitor.DisplayIndex)
            .FirstOrDefault();
    }

    private sealed record AssignmentIndex
    {
        public AssignmentIndex(
            IReadOnlyList<PresetAssignment> UniqueAssignments,
            IReadOnlyDictionary<string, PresetAssignment> ByMonitorKey)
        {
            ArgumentNullException.ThrowIfNull(UniqueAssignments);
            ArgumentNullException.ThrowIfNull(ByMonitorKey);

            this.UniqueAssignments = UniqueAssignments;
            this.ByMonitorKey = ByMonitorKey;
        }

        public IReadOnlyList<PresetAssignment> UniqueAssignments { get; }

        public IReadOnlyDictionary<string, PresetAssignment> ByMonitorKey { get; }
    }
}
