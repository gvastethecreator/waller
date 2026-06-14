namespace Waller.Native.Core.Models;

public enum MonitorApplyStatus
{
    Clean,
    Pending,
    Applying,
    Applied,
    Error,
}

public sealed record MonitorSession
{
    private MonitorSnapshot monitor = null!;
    private PresetAssignment desiredAssignment = null!;
    private MonitorApplyStatus applyStatus;

    public MonitorSession(
        MonitorSnapshot Monitor,
        PresetAssignment DesiredAssignment,
        PresetAssignment? LastAppliedAssignment,
        MonitorApplyStatus ApplyStatus,
        string? ApplyError,
        bool HasUnsavedPresetChanges,
        string? ApplyErrorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(Monitor);
        ArgumentNullException.ThrowIfNull(DesiredAssignment);

        this.Monitor = Monitor;
        this.DesiredAssignment = DesiredAssignment;
        this.LastAppliedAssignment = LastAppliedAssignment;
        this.ApplyStatus = ApplyStatus;
        this.ApplyError = ApplyError;
        this.ApplyErrorMessage = ApplyErrorMessage;
        this.HasUnsavedPresetChanges = HasUnsavedPresetChanges;
    }

    public MonitorSnapshot Monitor
    {
        get => monitor;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            monitor = value;
        }
    }

    public PresetAssignment DesiredAssignment
    {
        get => desiredAssignment;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            desiredAssignment = value;
        }
    }

    public PresetAssignment? LastAppliedAssignment { get; init; }

    public MonitorApplyStatus ApplyStatus
    {
        get => applyStatus;
        init
        {
            applyStatus = DefinedEnumValue.Require(
                value,
                nameof(value),
                "Monitor apply status is invalid.");
        }
    }

    public string? ApplyError { get; init; }

    public string? ApplyErrorMessage { get; init; }

    public bool HasUnsavedPresetChanges { get; init; }

    public static MonitorSession FromMonitor(MonitorSnapshot monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

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
        ArgumentNullException.ThrowIfNull(assignment);

        return this with
        {
            DesiredAssignment = assignment,
            ApplyStatus = MonitorApplyStatus.Pending,
            ApplyError = null,
            ApplyErrorMessage = null,
            HasUnsavedPresetChanges = hasUnsavedPresetChanges,
        };
    }

    public MonitorSession WithApplying()
    {
        return this with
        {
            ApplyStatus = MonitorApplyStatus.Applying,
            ApplyError = null,
            ApplyErrorMessage = null,
        };
    }

    public MonitorSession WithAppliedAssignment()
    {
        return this with
        {
            ApplyStatus = MonitorApplyStatus.Applied,
            LastAppliedAssignment = DesiredAssignment,
            ApplyError = null,
            ApplyErrorMessage = null,
        };
    }

    public MonitorSession WithApplyError(string error, string? errorMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return this with
        {
            ApplyStatus = MonitorApplyStatus.Error,
            ApplyError = error,
            ApplyErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage,
        };
    }
}
