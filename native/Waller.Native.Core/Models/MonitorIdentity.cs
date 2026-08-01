namespace Waller.Native.Core.Models;

public sealed record MonitorIdentity
{
    private string monitorKey = string.Empty;

    public MonitorIdentity(
        string MonitorKey,
        string? DeviceName,
        int DisplayIndex,
        int Width,
        int Height,
        int X,
        int Y)
    {
        this.MonitorKey = MonitorKey;
        this.DeviceName = DeviceName;
        this.DisplayIndex = DisplayIndex;
        this.Width = Width;
        this.Height = Height;
        this.X = X;
        this.Y = Y;
    }

    public string MonitorKey
    {
        get => monitorKey;
        init => monitorKey = value ?? string.Empty;
    }

    public string? DeviceName { get; init; }

    public int DisplayIndex { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public int X { get; init; }

    public int Y { get; init; }

    public MonitorBounds Bounds => new(X, Y, Width, Height);

    public bool IsValidForPresetAssignment =>
        !string.IsNullOrWhiteSpace(MonitorKey)
        && Width > 0
        && Height > 0;
}
