namespace Waller.Native.Core.Models;

public enum MonitorApplyStatus
{
    Clean,
    Pending,
    Applying,
    Applied,
    Error,
}

public sealed record MonitorSession(
    MonitorSnapshot Monitor,
    PresetAssignment DesiredAssignment,
    PresetAssignment? LastAppliedAssignment,
    MonitorApplyStatus ApplyStatus,
    string? ApplyError,
    bool HasUnsavedPresetChanges)
{
    public static MonitorSession FromMonitor(MonitorSnapshot monitor)
    {
        var assignment = PresetAssignments.Normalize(new PresetAssignment(
            monitor.Identity,
            monitor.CurrentSource,
            monitor.CurrentPlacement ?? WallpaperPlacement.Default));

        return new MonitorSession(
            monitor,
            assignment,
            assignment,
            MonitorApplyStatus.Clean,
            null,
            false);
    }

    public MonitorSession WithPendingAssignment(PresetAssignment assignment, bool hasUnsavedPresetChanges)
    {
        return this with
        {
            DesiredAssignment = assignment,
            ApplyStatus = MonitorApplyStatus.Pending,
            ApplyError = null,
            HasUnsavedPresetChanges = hasUnsavedPresetChanges,
        };
    }

    public MonitorSession WithApplying()
    {
        return this with
        {
            ApplyStatus = MonitorApplyStatus.Applying,
            ApplyError = null,
        };
    }

    public MonitorSession WithAppliedAssignment()
    {
        return this with
        {
            ApplyStatus = MonitorApplyStatus.Applied,
            LastAppliedAssignment = DesiredAssignment,
            ApplyError = null,
        };
    }

    public MonitorSession WithApplyError(string error)
    {
        return this with
        {
            ApplyStatus = MonitorApplyStatus.Error,
            ApplyError = error,
        };
    }
}
