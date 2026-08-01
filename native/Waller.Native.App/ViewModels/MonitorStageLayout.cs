using Waller.Native.Core.Models;
using Waller.Native.Core.Topology;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorStageLayout(
    double SurfaceWidth,
    double SurfaceHeight,
    IReadOnlyList<MonitorTopologyTile> Tiles)
{
    private const double StageWidth = 960;
    private const double StageHeight = 312;
    private const double SurfacePadding = 16;
    private const double MaximumContentWidth = StageWidth - (SurfacePadding * 2);
    private const double MaximumContentHeight = StageHeight - (SurfacePadding * 2);

    public static MonitorStageLayout Calculate(IReadOnlyList<MonitorBounds> bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (bounds.Count == 0)
        {
            return new(StageWidth, StageHeight, []);
        }

        var physicalLayout = MonitorTopologyLayout.Calculate(
            bounds,
            maxWidth: MaximumContentWidth,
            maxHeight: MaximumContentHeight,
            minSurfaceWidth: 96,
            minSurfaceHeight: 48);
        var tiles = bounds
            .Select(monitor => physicalLayout.TileFor(monitor, minTileWidth: 1, minTileHeight: 1))
            .Select(tile => new MonitorTopologyTile(
                tile.Left + SurfacePadding,
                tile.Top + SurfacePadding,
                tile.Width,
                tile.Height))
            .ToArray();

        return new(
            physicalLayout.SurfaceWidth + (SurfacePadding * 2),
            physicalLayout.SurfaceHeight + (SurfacePadding * 2),
            tiles);
    }
}
