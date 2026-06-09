namespace Waller.Native.Core.Models;

public sealed record MonitorIdentity(
    string MonitorKey,
    string? DeviceName,
    int DisplayIndex,
    int Width,
    int Height,
    int X,
    int Y)
{
    public MonitorBounds Bounds => new(X, Y, Width, Height);

    public bool IsValidForPresetAssignment =>
        !string.IsNullOrWhiteSpace(MonitorKey)
        && Width > 0
        && Height > 0;
}
