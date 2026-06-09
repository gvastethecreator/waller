using Waller.Native.Core.Models;

namespace Waller.Native.Core.Topology;

public sealed record MonitorTopologyLayout(
    double SurfaceWidth,
    double SurfaceHeight,
    int MinX,
    int MinY,
    double Scale)
{
    public static MonitorTopologyLayout Calculate(
        IReadOnlyList<MonitorBounds> bounds,
        double maxWidth = 720,
        double maxHeight = 96,
        double minSurfaceWidth = 96,
        double minSurfaceHeight = 48)
    {
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
        new(
            (bounds.X - MinX) * Scale,
            (bounds.Y - MinY) * Scale,
            Math.Max(minTileWidth, bounds.Width * Scale),
            Math.Max(minTileHeight, bounds.Height * Scale));
}

public sealed record MonitorTopologyTile(
    double Left,
    double Top,
    double Width,
    double Height);
