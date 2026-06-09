namespace Waller.Native.Core.Models;

public sealed record ActiveSession(
    IReadOnlyList<MonitorSession> Monitors,
    PresetIdentity? BasedOnPreset,
    bool HasUnsavedPresetChanges,
    IReadOnlyList<PresetAssignment> MissingAssignments)
{
    public static ActiveSession FromMonitors(IReadOnlyList<MonitorSnapshot> monitors)
    {
        return new ActiveSession(
            monitors.Select(MonitorSession.FromMonitor).ToList(),
            null,
            false,
            Array.Empty<PresetAssignment>());
    }

    public ActiveSession WithSavedPreset(PresetIdentity preset)
    {
        return this with
        {
            BasedOnPreset = preset,
            HasUnsavedPresetChanges = false,
            Monitors = Monitors.Select(monitor => monitor with
            {
                HasUnsavedPresetChanges = false,
            }).ToList(),
        };
    }
}
