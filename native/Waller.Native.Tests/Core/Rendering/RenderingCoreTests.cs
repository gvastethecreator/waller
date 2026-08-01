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
    public async Task SolidColorPngWriter_KeepsExistingFileWhenAtomicWriteIsCancelled()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "wallpaper.png");
            await File.WriteAllBytesAsync(path, [9, 8, 7]);
            var pixels = PixelBuffer.CreateSolid(2, 2, RgbColor.Black);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var error = await Record.ExceptionAsync(() => SolidColorPngWriter.WriteAsync(path, pixels, cts.Token));

            Assert.IsAssignableFrom<OperationCanceledException>(error);
            Assert.Equal([9, 8, 7], await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SolidColorPngWriter_WritesCompletePngThroughAtomicPath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "wallpaper.png");
            var pixels = PixelBuffer.CreateSolid(3, 2, new RgbColor(1, 2, 3));

            await SolidColorPngWriter.WriteAsync(path, pixels);
            var size = ReadPngSize(path);

            Assert.Equal((3, 2), size);
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SolidColorPngWriter_RejectsNullPixels()
    {
        PixelBuffer? pixels = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SolidColorPngWriter.WriteAsync("wallpaper.png", pixels!));

        Assert.Equal("pixels", error.ParamName);
    }

    [Fact]
    public void PixelBuffer_RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(0, 1, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(1, 0, []));
    }

    [Fact]
    public void PixelBuffer_RejectsNullData()
    {
        byte[]? data = null;

        var error = Assert.Throws<ArgumentNullException>(() => new PixelBuffer(1, 1, data!));

        Assert.Equal("data", error.ParamName);
    }

    [Fact]
    public void PixelBuffer_RejectsInvalidDataLength()
    {
        var error = Assert.Throws<ArgumentException>(() => new PixelBuffer(2, 2, new byte[3]));

        Assert.Equal("data", error.ParamName);
    }

    [Fact]
    public void PixelBuffer_CopiesInputData()
    {
        var data = new byte[] { 1, 2, 3 };
        var buffer = new PixelBuffer(1, 1, data);

        data[0] = 9;

        Assert.Equal(new RgbColor(1, 2, 3), buffer.GetPixel(0, 0));
    }

    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(1, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 1, "y")]
    public void PixelBuffer_GetPixelRejectsOutOfBoundsCoordinates(int x, int y, string parameterName)
    {
        var buffer = new PixelBuffer(1, 1, new byte[3]);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(x, y));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(1, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 1, "y")]
    public void PixelBuffer_SetPixelRejectsOutOfBoundsCoordinates(int x, int y, string parameterName)
    {
        var buffer = new PixelBuffer(1, 1, new byte[3]);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            buffer.SetPixel(x, y, RgbColor.Black));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("monitor")]
    [InlineData("path")]
    [InlineData("width")]
    [InlineData("height")]
    public void RenderedWallpaper_RejectsInvalidInputs(string parameterName)
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var maybeMonitor = parameterName == "monitor" ? null : monitor;
        var path = parameterName == "path" ? " " : @"C:\Wallpapers\rendered.png";
        var width = parameterName == "width" ? 0 : 1920;
        var height = parameterName == "height" ? 0 : 1080;

        var error = Assert.ThrowsAny<ArgumentException>(() =>
            new RenderedWallpaper(maybeMonitor!, path, width, height, DateTimeOffset.UtcNow));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void RenderedWallpaper_RejectsRelativePath()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var error = Assert.Throws<ArgumentException>(() =>
            new RenderedWallpaper(monitor, "rendered.png", 1920, 1080, DateTimeOffset.UtcNow));

        Assert.Equal("path", error.ParamName);
        Assert.Contains("Rendered wallpaper path must be absolute.", error.Message);
    }

    [Theory]
    [InlineData("monitor")]
    [InlineData("assignment")]
    public void RenderRequest_RejectsNullInputs(string parameterName)
    {
        var monitor = CreateMonitor("DISPLAY-1", 32, 18, WallpaperSource.Empty);
        var assignment = new PresetAssignment(
            monitor.Identity,
            WallpaperSource.Empty,
            WallpaperPlacement.Default);
        MonitorSnapshot? maybeMonitor = parameterName == "monitor" ? null : monitor;
        PresetAssignment? maybeAssignment = parameterName == "assignment" ? null : assignment;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new RenderRequest(maybeMonitor!, maybeAssignment!));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_RendersSolidColorAtMonitorSize()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 32, 18, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromSolidColor("#336699"),
                WallpaperPlacement.Default);

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var (width, height) = ReadPngSize(rendered.Path);

            Assert.True(File.Exists(rendered.Path));
            Assert.Equal(32, width);
            Assert.Equal(18, height);
            Assert.Equal(32, rendered.Width);
            Assert.Equal(18, rendered.Height);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void BasicPngWallpaperRenderer_RejectsNullStore()
    {
        RenderedWallpaperStore? store = null;

        var error = Assert.Throws<ArgumentNullException>(() => new BasicPngWallpaperRenderer(store!));

        Assert.Equal("store", error.ParamName);
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_RejectsNullRenderRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            RenderRequest? request = null;

            var error = await Assert.ThrowsAsync<ArgumentNullException>(() => renderer.RenderAsync(request!));

            Assert.Equal("request", error.ParamName);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_ThrowsStableErrorForMissingImageSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 32, 18, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(@"C:\missing-image.png"),
                WallpaperPlacement.Default);

            var error = await Assert.ThrowsAsync<WallpaperRenderException>(
                () => renderer.RenderAsync(new RenderRequest(monitor, assignment)));

            Assert.Equal(ApplyErrorCodes.MissingImageSource, error.ErrorCode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_StretchesImageSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 4, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.Center));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(1, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(2, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(3, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_TilesImageSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 5, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Tile, WallpaperAnchor.Center));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(1, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(2, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(3, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(4, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_CoverUsesAnchorForCrop()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 1, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Right));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(0, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_CoverUsesOffsetForCrop()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteFourColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 2, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, -100, 0));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(0, 0, 255), pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(255, 255, 255), pixels.GetPixel(1, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_ContainKeepsBlackBands()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 4, 4, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(RgbColor.Black, pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(0, 1));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(3, 2));
            Assert.Equal(RgbColor.Black, pixels.GetPixel(3, 3));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesCoverCropWithOffset()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 4,
            sourceHeight: 1,
            targetWidth: 2,
            targetHeight: 1,
            new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, -100, 0));

        Assert.False(plan.IsTile);
        Assert.Equal(-2, plan.OriginX);
        Assert.Equal(0, plan.OriginY);
        Assert.Equal(4, plan.DrawWidth);
        Assert.Equal(1, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesContainBlackBandArea()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 4,
            targetHeight: 4,
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center));

        Assert.False(plan.IsTile);
        Assert.Equal(0, plan.OriginX);
        Assert.Equal(1, plan.OriginY);
        Assert.Equal(4, plan.DrawWidth);
        Assert.Equal(2, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_MarksTileWithoutScaling()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 5,
            targetHeight: 3,
            new WallpaperPlacement(WallpaperFitMode.Tile, WallpaperAnchor.BottomRight, 100, -100));

        Assert.True(plan.IsTile);
        Assert.Equal(0, plan.OriginX);
        Assert.Equal(0, plan.OriginY);
        Assert.Equal(2, plan.DrawWidth);
        Assert.Equal(1, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesStretchToTarget()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 5,
            targetHeight: 3,
            new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.BottomRight));

        Assert.False(plan.IsTile);
        Assert.Equal(0, plan.OriginX);
        Assert.Equal(0, plan.OriginY);
        Assert.Equal(5, plan.DrawWidth);
        Assert.Equal(3, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesCenterWithoutScaling()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 6,
            targetHeight: 5,
            new WallpaperPlacement(WallpaperFitMode.Center, WallpaperAnchor.Center));

        Assert.False(plan.IsTile);
        Assert.Equal(2, plan.OriginX);
        Assert.Equal(2, plan.OriginY);
        Assert.Equal(2, plan.DrawWidth);
        Assert.Equal(1, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_RejectsNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 0,
                sourceHeight: 1,
                targetWidth: 2,
                targetHeight: 2,
                WallpaperPlacement.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 0,
                targetWidth: 2,
                targetHeight: 2,
                WallpaperPlacement.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 1,
                targetWidth: 0,
                targetHeight: 2,
                WallpaperPlacement.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 1,
                targetWidth: 2,
                targetHeight: 0,
                WallpaperPlacement.Default));
    }

    [Theory]
    [InlineData("DrawWidth")]
    [InlineData("DrawHeight")]
    public void ImagePlacementPlan_RejectsInvalidDirectDrawDimensions(string parameterName)
    {
        var drawWidth = parameterName == "DrawWidth" ? 0 : 2;
        var drawHeight = parameterName == "DrawHeight" ? 0 : 1;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ImagePlacementPlan(false, 0, 0, drawWidth, drawHeight));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("DrawWidth")]
    [InlineData("DrawHeight")]
    public void ImagePlacementPlan_WithExpressionRejectsInvalidDrawDimensions(string propertyName)
    {
        var plan = new ImagePlacementPlan(false, 0, 0, 2, 1);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => propertyName == "DrawWidth"
            ? plan with { DrawWidth = 0 }
            : plan with { DrawHeight = 0 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void ImagePlacementPlan_RejectsNullPlacement()
    {
        WallpaperPlacement? placement = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 1,
                targetWidth: 2,
                targetHeight: 2,
                placement!));

        Assert.Equal("placement", error.ParamName);
    }

    [Theory]
    [InlineData("source")]
    [InlineData("placement")]
    public void ImagePlacementRenderer_RejectsNullInputs(string parameterName)
    {
        var source = new PixelBuffer(1, 1, new byte[3]);
        PixelBuffer? maybeSource = parameterName == "source" ? null : source;
        WallpaperPlacement? maybePlacement = parameterName == "placement" ? null : WallpaperPlacement.Default;

        var error = Assert.Throws<ArgumentNullException>(() =>
            ImagePlacementRenderer.Render(maybeSource!, 2, 2, maybePlacement!));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void WallpaperRenderException_NormalizesUnknownErrorCode()
    {
        var error = new WallpaperRenderException("driver exploded", "render failed");

        Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, error.ErrorCode);
    }
}
