using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Storage;
using Waller.Native.Core.Topology;
using Waller.Native.Core.Windows;

namespace Waller.Native.Tests;

public sealed partial class CoreArchitectureTests
{

    [Fact]
    public void MonitorIdentityMatcher_RejectsFallbackOutsidePositionTolerance()
    {
        var currentMonitor = new MonitorIdentity("DISPLAY-NEW", "Current", 1, 1920, 1080, 0, 0);
        var tooFar = new MonitorIdentity("DISPLAY-OLD", "Saved", 1, 1920, 1080, 33, 0);

        Assert.False(MonitorIdentityMatcher.IsFallbackCandidate(tooFar, currentMonitor));
    }

    [Fact]
    public void MonitorTopologyLayout_ScalesNegativeCoordinateTopology()
    {
        var left = new MonitorBounds(-1920, 0, 1920, 1080);
        var primary = new MonitorBounds(0, 0, 2560, 1440);

        var layout = MonitorTopologyLayout.Calculate([left, primary]);
        var leftTile = layout.TileFor(left);
        var primaryTile = layout.TileFor(primary);

        Assert.Equal(-1920, layout.MinX);
        Assert.Equal(0, layout.MinY);
        Assert.True(layout.SurfaceWidth <= 720);
        Assert.True(layout.SurfaceHeight <= 96);
        Assert.Equal(0, leftTile.Left);
        Assert.True(primaryTile.Left > leftTile.Left);
        Assert.Equal(leftTile.Top, primaryTile.Top);
        Assert.True(primaryTile.Width > leftTile.Width);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorBounds_RejectsNonPositiveDimensions(string parameterName)
    {
        var width = parameterName == "Width" ? 0 : 1920;
        var height = parameterName == "Height" ? 0 : 1080;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonitorBounds(0, 0, width, height));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorBounds_WithExpressionRejectsNonPositiveDimensions(string parameterName)
    {
        var bounds = new MonitorBounds(0, 0, 1920, 1080);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => parameterName == "Width"
            ? bounds with { Width = 0 }
            : bounds with { Height = 0 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorTopologyLayout_UsesStableEmptySurface()
    {
        var layout = MonitorTopologyLayout.Calculate([]);

        Assert.Equal(720, layout.SurfaceWidth);
        Assert.Equal(96, layout.SurfaceHeight);
        Assert.Equal(0, layout.MinX);
        Assert.Equal(0, layout.MinY);
        Assert.Equal(1, layout.Scale);
    }

    [Theory]
    [InlineData("SurfaceWidth")]
    [InlineData("SurfaceHeight")]
    [InlineData("Scale")]
    public void MonitorTopologyLayout_RejectsInvalidDirectValues(string parameterName)
    {
        double surfaceWidth = parameterName == "SurfaceWidth" ? 0 : 720;
        double surfaceHeight = parameterName == "SurfaceHeight" ? 0 : 96;
        double scale = parameterName == "Scale" ? 0 : 1;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonitorTopologyLayout(surfaceWidth, surfaceHeight, 0, 0, scale));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("SurfaceWidth")]
    [InlineData("SurfaceHeight")]
    [InlineData("Scale")]
    public void MonitorTopologyLayout_WithExpressionRejectsInvalidValues(string propertyName)
    {
        var layout = new MonitorTopologyLayout(720, 96, 0, 0, 1);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => propertyName switch
        {
            "SurfaceWidth" => layout with { SurfaceWidth = 0 },
            "SurfaceHeight" => layout with { SurfaceHeight = 0 },
            _ => layout with { Scale = 0 },
        });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorTopologyLayout_RejectsNullBoundsList()
    {
        IReadOnlyList<MonitorBounds>? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => MonitorTopologyLayout.Calculate(bounds!));

        Assert.Equal("bounds", error.ParamName);
    }

    [Theory]
    [InlineData("maxWidth")]
    [InlineData("maxHeight")]
    [InlineData("minSurfaceWidth")]
    [InlineData("minSurfaceHeight")]
    public void MonitorTopologyLayout_RejectsInvalidSurfaceDimensions(string parameterName)
    {
        var bounds = new[] { new MonitorBounds(0, 0, 1920, 1080) };
        double maxWidth = parameterName == "maxWidth" ? 0 : 720;
        double maxHeight = parameterName == "maxHeight" ? 0 : 96;
        double minSurfaceWidth = parameterName == "minSurfaceWidth" ? 0 : 96;
        double minSurfaceHeight = parameterName == "minSurfaceHeight" ? 0 : 48;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => MonitorTopologyLayout.Calculate(
            bounds,
            maxWidth,
            maxHeight,
            minSurfaceWidth,
            minSurfaceHeight));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void MonitorTopologyLayout_TileForRejectsNullBounds()
    {
        var layout = MonitorTopologyLayout.Calculate([]);
        MonitorBounds? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => layout.TileFor(bounds!));

        Assert.Equal("bounds", error.ParamName);
    }

    [Theory]
    [InlineData("minTileWidth")]
    [InlineData("minTileHeight")]
    public void MonitorTopologyLayout_TileForRejectsInvalidTileDimensions(string parameterName)
    {
        var layout = MonitorTopologyLayout.Calculate([]);
        var bounds = new MonitorBounds(0, 0, 1920, 1080);
        double minTileWidth = parameterName == "minTileWidth" ? 0 : 48;
        double minTileHeight = parameterName == "minTileHeight" ? 0 : 28;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => layout.TileFor(
            bounds,
            minTileWidth,
            minTileHeight));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorTopologyTile_RejectsInvalidDirectDimensions(string parameterName)
    {
        double width = parameterName == "Width" ? 0 : 48;
        double height = parameterName == "Height" ? 0 : 28;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonitorTopologyTile(0, 0, width, height));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorTopologyTile_WithExpressionRejectsInvalidDimensions(string propertyName)
    {
        var tile = new MonitorTopologyTile(0, 0, 48, 28);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => propertyName == "Width"
            ? tile with { Width = 0 }
            : tile with { Height = 0 });

        Assert.Equal("value", error.ParamName);
    }
}
