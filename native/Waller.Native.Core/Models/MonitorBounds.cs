namespace Waller.Native.Core.Models;

public sealed record MonitorBounds(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;
}
