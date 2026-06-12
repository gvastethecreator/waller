namespace Waller.Native.Core.Models;

public sealed record MonitorBounds
{
    private int width;
    private int height;

    public MonitorBounds(int X, int Y, int Width, int Height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);

        this.X = X;
        this.Y = Y;
        width = Width;
        height = Height;
    }

    public int X { get; init; }

    public int Y { get; init; }

    public int Width
    {
        get => width;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            width = value;
        }
    }

    public int Height
    {
        get => height;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            height = value;
        }
    }

    public int Right => X + Width;

    public int Bottom => Y + Height;
}
