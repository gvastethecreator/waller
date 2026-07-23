using Waller.Native.Core.Models;
using Waller.Native.Core.Topology;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorStageLayout(
    double SurfaceWidth,
    double SurfaceHeight,
    IReadOnlyList<MonitorTopologyTile> Tiles)
{
    private const double StageWidth = 720;
    private const double StageHeight = 128;
    private const double TileGap = 12;

    public static MonitorStageLayout Calculate(IReadOnlyList<MonitorBounds> bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (bounds.Count == 0)
        {
            return new(StageWidth, StageHeight, []);
        }

        var totalAspectRatio = bounds.Sum(item => (double)item.Width / item.Height);
        var availableWidth = StageWidth - (TileGap * (bounds.Count - 1));
        var tileHeight = Math.Min(StageHeight, availableWidth / totalAspectRatio);
        var currentLeft = 0d;
        var tiles = new List<MonitorTopologyTile>(bounds.Count);

        foreach (var monitor in bounds)
        {
            var width = ((double)monitor.Width / monitor.Height) * tileHeight;
            tiles.Add(new MonitorTopologyTile(currentLeft, 0, width, tileHeight));
            currentLeft += width + TileGap;
        }

        return new(StageWidth, StageHeight, tiles);
    }
}
