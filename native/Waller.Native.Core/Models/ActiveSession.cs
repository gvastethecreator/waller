namespace Waller.Native.Core.Models;

public sealed record ActiveSession
{
    private IReadOnlyList<MonitorSession> monitors = [];
    private IReadOnlyList<PresetAssignment> missingAssignments = [];

    public ActiveSession(
        IReadOnlyList<MonitorSession> Monitors,
        PresetIdentity? BasedOnPreset,
        bool HasUnsavedPresetChanges,
        IReadOnlyList<PresetAssignment> MissingAssignments)
    {
        monitors = RequiredList.Copy(Monitors, nameof(Monitors), "Active Session monitor list cannot include null items.");
        this.BasedOnPreset = BasedOnPreset;
        this.HasUnsavedPresetChanges = HasUnsavedPresetChanges;
        missingAssignments = RequiredList.Copy(
            MissingAssignments,
            nameof(MissingAssignments),
            "Active Session missing assignment list cannot include null items.");
    }

    public IReadOnlyList<MonitorSession> Monitors
    {
        get => monitors;
        init
        {
            monitors = RequiredList.Copy(
                value,
                nameof(value),
                "Active Session monitor list cannot include null items.");
        }
    }

    public PresetIdentity? BasedOnPreset { get; init; }

    public bool HasUnsavedPresetChanges { get; init; }

    public IReadOnlyList<PresetAssignment> MissingAssignments
    {
        get => missingAssignments;
        init
        {
            missingAssignments = RequiredList.Copy(
                value,
                nameof(value),
                "Active Session missing assignment list cannot include null items.");
        }
    }

    public static ActiveSession FromMonitors(IReadOnlyList<MonitorSnapshot> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        RequiredList.ValidateItems(monitors, nameof(monitors), "Active Session monitor snapshot list cannot include null items.");

        return new ActiveSession(
            monitors.Select(MonitorSession.FromMonitor).ToList(),
            null,
            false,
            Array.Empty<PresetAssignment>());
    }

    public ActiveSession WithSavedPreset(PresetIdentity preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

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
