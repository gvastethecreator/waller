using Waller.Native.Core.Presets;

namespace Waller.Native.Core.Models;

public sealed record PresetIdentity
{
    private string name = string.Empty;

    public PresetIdentity(Guid Id, string Name)
    {
        this.Id = PresetIds.RequireValid(Id, nameof(Id));
        this.Name = PresetNames.Validate(Name, nameof(Name));
    }

    private Guid id;

    public Guid Id
    {
        get => id;
        init => id = PresetIds.RequireValid(value, nameof(value));
    }

    public string Name
    {
        get => name;
        init
        {
            name = PresetNames.Validate(value, nameof(value));
        }
    }
}

public sealed record Preset
{
    private string name = string.Empty;
    private IReadOnlyList<PresetAssignment> assignments = [];

    public Preset(
        int SchemaVersion,
        Guid Id,
        string Name,
        IReadOnlyList<PresetAssignment> Assignments,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        this.SchemaVersion = SchemaVersion;
        this.Id = PresetIds.RequireValid(Id, nameof(Id));
        this.Name = PresetNames.Validate(Name, nameof(Name));
        assignments = RequiredList.Copy(
            Assignments,
            nameof(Assignments),
            "Preset assignment list cannot include null items.");
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; }

    private Guid id;

    public Guid Id
    {
        get => id;
        init => id = PresetIds.RequireValid(value, nameof(value));
    }

    public string Name
    {
        get => name;
        init
        {
            name = PresetNames.Validate(value, nameof(value));
        }
    }

    public IReadOnlyList<PresetAssignment> Assignments
    {
        get => assignments;
        init
        {
            assignments = RequiredList.Copy(
                value,
                nameof(value),
                "Preset assignment list cannot include null items.");
        }
    }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public PresetIdentity Identity => new(Id, Name);
}

public sealed record PresetAssignment
{
    private MonitorIdentity savedMonitor = null!;
    private WallpaperSource source = null!;
    private WallpaperPlacement placement = null!;

    public PresetAssignment(
        MonitorIdentity SavedMonitor,
        WallpaperSource Source,
        WallpaperPlacement Placement)
    {
        ArgumentNullException.ThrowIfNull(SavedMonitor);
        ArgumentNullException.ThrowIfNull(Source);
        ArgumentNullException.ThrowIfNull(Placement);

        this.SavedMonitor = SavedMonitor;
        this.Source = Source;
        this.Placement = Placement;
    }

    public MonitorIdentity SavedMonitor
    {
        get => savedMonitor;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            savedMonitor = value;
        }
    }

    public WallpaperSource Source
    {
        get => source;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            source = value;
        }
    }

    public WallpaperPlacement Placement
    {
        get => placement;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            placement = value;
        }
    }
}
