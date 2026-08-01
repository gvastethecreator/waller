using Waller.Native.Core.Models;

namespace Waller.Native.Core.Topology;

public sealed record MonitorTopologyLayout
{
    private double surfaceWidth;
    private double surfaceHeight;
    private double scale;

    public MonitorTopologyLayout(
        double SurfaceWidth,
        double SurfaceHeight,
        int MinX,
        int MinY,
        double Scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SurfaceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SurfaceHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Scale);

        surfaceWidth = SurfaceWidth;
        surfaceHeight = SurfaceHeight;
        this.MinX = MinX;
        this.MinY = MinY;
        scale = Scale;
    }

    public double SurfaceWidth
    {
        get => surfaceWidth;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            surfaceWidth = value;
        }
    }

    public double SurfaceHeight
    {
        get => surfaceHeight;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            surfaceHeight = value;
        }
    }

    public int MinX { get; init; }

    public int MinY { get; init; }

    public double Scale
    {
        get => scale;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            scale = value;
        }
    }

    public static MonitorTopologyLayout Calculate(
        IReadOnlyList<MonitorBounds> bounds,
        double maxWidth = 720,
        double maxHeight = 96,
        double minSurfaceWidth = 96,
        double minSurfaceHeight = 48)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minSurfaceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minSurfaceHeight);

        if (bounds.Count == 0)
        {
            return new MonitorTopologyLayout(maxWidth, maxHeight, 0, 0, 1);
        }

        var minX = bounds.Min(item => item.X);
        var minY = bounds.Min(item => item.Y);
        var maxX = bounds.Max(item => item.Right);
        var maxY = bounds.Max(item => item.Bottom);
        var virtualWidth = Math.Max(1, maxX - minX);
        var virtualHeight = Math.Max(1, maxY - minY);
        var scale = Math.Min(maxWidth / virtualWidth, maxHeight / virtualHeight);

        return new MonitorTopologyLayout(
            Math.Max(minSurfaceWidth, virtualWidth * scale),
            Math.Max(minSurfaceHeight, virtualHeight * scale),
            minX,
            minY,
            scale);
    }

    public MonitorTopologyTile TileFor(
        MonitorBounds bounds,
        double minTileWidth = 48,
        double minTileHeight = 28) =>
        CreateTile(bounds, minTileWidth, minTileHeight);

    private MonitorTopologyTile CreateTile(
        MonitorBounds bounds,
        double minTileWidth,
        double minTileHeight)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minTileWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minTileHeight);

        return new MonitorTopologyTile(
            (bounds.X - MinX) * Scale,
            (bounds.Y - MinY) * Scale,
            Math.Max(minTileWidth, bounds.Width * Scale),
            Math.Max(minTileHeight, bounds.Height * Scale));
    }
}

public sealed record MonitorTopologyTile
{
    private double width;
    private double height;

    public MonitorTopologyTile(double Left, double Top, double Width, double Height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);

        this.Left = Left;
        this.Top = Top;
        width = Width;
        height = Height;
    }

    public double Left { get; init; }

    public double Top { get; init; }

    public double Width
    {
        get => width;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            width = value;
        }
    }

    public double Height
    {
        get => height;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            height = value;
        }
    }
}
