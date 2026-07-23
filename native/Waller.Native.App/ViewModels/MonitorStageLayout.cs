using Waller.Native.Core.Models;
using Waller.Native.Core.Topology;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorStageLayout(
    double SurfaceWidth,
    double SurfaceHeight,
    IReadOnlyList<MonitorTopologyTile> Tiles)
{
    private const double StageWidth = 960;
    private const double StageHeight = 176;
    private const double TileGap = 16;
    private const double MaximumVerticalOffset = 32;

    public static MonitorStageLayout Calculate(IReadOnlyList<MonitorBounds> bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (bounds.Count == 0)
        {
            return new(StageWidth, StageHeight, []);
        }

        var orderedIndexes = Enumerable
            .Range(0, bounds.Count)
            .OrderBy(index => bounds[index].X)
            .ThenBy(index => bounds[index].Y)
            .ToArray();
        var totalAspectRatio = bounds.Sum(item => (double)item.Width / item.Height);
        var availableWidth = StageWidth - (TileGap * (bounds.Count - 1));
        var tileHeight = Math.Min(StageHeight, availableWidth / totalAspectRatio);
        var currentLeft = 0d;
        var minY = bounds.Min(item => item.Y);
        var maxY = bounds.Max(item => item.Y);
        var yRange = maxY - minY;
        var tiles = new MonitorTopologyTile[bounds.Count];

        foreach (var index in orderedIndexes)
        {
            var monitor = bounds[index];
            var width = ((double)monitor.Width / monitor.Height) * tileHeight;
            var top = yRange == 0
                ? 0
                : ((double)(monitor.Y - minY) / yRange) * MaximumVerticalOffset;
            tiles[index] = new MonitorTopologyTile(currentLeft, top, width, tileHeight);
            currentLeft += width + TileGap;
        }

        return new(StageWidth, tileHeight + MaximumVerticalOffset, tiles);
    }
}
