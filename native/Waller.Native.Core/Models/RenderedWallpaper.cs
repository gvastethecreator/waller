namespace Waller.Native.Core.Models;

public sealed record RenderedWallpaper
{
    public RenderedWallpaper(
        MonitorIdentity monitor,
        string path,
        int width,
        int height,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!System.IO.Path.IsPathRooted(path))
        {
            throw new ArgumentException("Rendered wallpaper path must be absolute.", nameof(path));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Monitor = monitor;
        Path = path;
        Width = width;
        Height = height;
        CreatedAt = createdAt;
    }

    public MonitorIdentity Monitor { get; }

    public string Path { get; }

    public int Width { get; }

    public int Height { get; }

    public DateTimeOffset CreatedAt { get; }
}

public sealed record RenderRequest
{
    public RenderRequest(MonitorSnapshot monitor, PresetAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(assignment);

        Monitor = monitor;
        Assignment = assignment;
    }

    public MonitorSnapshot Monitor { get; }

    public PresetAssignment Assignment { get; }
}
