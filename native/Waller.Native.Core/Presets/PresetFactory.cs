using Waller.Native.Core.Models;

namespace Waller.Native.Core.Presets;

public static class PresetFactory
{
    public static Preset CreateFromSession(ActiveSession session, string name)
    {
        ArgumentNullException.ThrowIfNull(session);

        return CreateFromSession(session, Guid.NewGuid(), name, createdAt: default);
    }

    public static Preset UpdateFromSession(ActiveSession session, PresetIdentity identity, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);

        return CreateFromSession(session, identity.Id, identity.Name, createdAt);
    }

    public static Preset Duplicate(Preset preset, string name)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var now = DateTimeOffset.UtcNow;
        return preset with
        {
            Id = Guid.NewGuid(),
            Name = PresetNames.Validate(name),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static Preset Rename(Preset preset, string name)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return preset with { Name = PresetNames.Validate(name) };
    }

    private static Preset CreateFromSession(
        ActiveSession session,
        Guid id,
        string name,
        DateTimeOffset createdAt)
    {
        var now = DateTimeOffset.UtcNow;
        var assignments = session.Monitors
            .Select(monitor => monitor.DesiredAssignment with
            {
                SavedMonitor = monitor.Monitor.Identity,
            })
            .Concat(session.MissingAssignments)
            .ToList();

        return new Preset(
            Preset.CurrentSchemaVersion,
            id,
            name,
            PresetAssignments.Normalize(assignments),
            createdAt == default ? now : createdAt,
            now);
    }
}
