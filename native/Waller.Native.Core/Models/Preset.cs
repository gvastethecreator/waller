namespace Waller.Native.Core.Models;

public sealed record PresetIdentity(Guid Id, string Name);

public sealed record Preset(
    int SchemaVersion,
    Guid Id,
    string Name,
    IReadOnlyList<PresetAssignment> Assignments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public const int CurrentSchemaVersion = 1;

    public PresetIdentity Identity => new(Id, Name);
}

public sealed record PresetAssignment(
    MonitorIdentity SavedMonitor,
    WallpaperSource Source,
    WallpaperPlacement Placement);
