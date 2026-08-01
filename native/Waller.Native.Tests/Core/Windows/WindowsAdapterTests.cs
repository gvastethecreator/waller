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
    public void DesktopWallpaperInterop_MapsEmptyWallpaperPathToEmptySource()
    {
        var source = DesktopWallpaperInterop.WallpaperPathToSource("   ");

        Assert.Equal(WallpaperSourceKind.Empty, source.Kind);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsWallpaperPathToImageSource()
    {
        var source = DesktopWallpaperInterop.WallpaperPathToSource(@" C:\Wallpapers\current.jpg ");

        Assert.Equal(WallpaperSourceKind.Image, source.Kind);
        Assert.Equal(@"C:\Wallpapers\current.jpg", source.ImagePath);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsInvalidWallpaperPathToEmptySource()
    {
        var source = DesktopWallpaperInterop.WallpaperPathToSource("relative\\wallpaper.jpg");

        Assert.Equal(WallpaperSourceKind.Empty, source.Kind);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsWindowsBackgroundColorToSolidSource()
    {
        var source = DesktopWallpaperInterop.BackgroundColorResultToSource(0, 0x00332211);

        Assert.Equal(WallpaperSourceKind.SolidColor, source.Kind);
        Assert.Equal("#112233", source.ColorHex);
    }

    [Fact]
    public void DesktopWallpaperInterop_FallsBackWhenWindowsBackgroundColorReadFails()
    {
        var source = DesktopWallpaperInterop.BackgroundColorResultToSource(
            unchecked((int)0x80004005),
            0x00332211);

        Assert.Equal(WallpaperSourceKind.Empty, source.Kind);
    }

    [Theory]
    [InlineData(DesktopWallpaperPosition.Center, WallpaperFitMode.Center)]
    [InlineData(DesktopWallpaperPosition.Tile, WallpaperFitMode.Tile)]
    [InlineData(DesktopWallpaperPosition.Stretch, WallpaperFitMode.Stretch)]
    [InlineData(DesktopWallpaperPosition.Fit, WallpaperFitMode.Contain)]
    [InlineData(DesktopWallpaperPosition.Fill, WallpaperFitMode.Cover)]
    [InlineData(DesktopWallpaperPosition.Span, WallpaperFitMode.Cover)]
    public void DesktopWallpaperInterop_MapsWindowsPositionToPlacement(
        DesktopWallpaperPosition position,
        WallpaperFitMode fitMode)
    {
        var placement = DesktopWallpaperInterop.PositionToPlacement(position);

        Assert.Equal(fitMode, placement.FitMode);
        Assert.Equal(WallpaperAnchor.Center, placement.Anchor);
    }

    [Fact]
    public void DesktopWallpaperInterop_FallsBackWhenWindowsPositionReadFails()
    {
        var placement = DesktopWallpaperInterop.PositionResultToPlacement(
            unchecked((int)0x80004005),
            DesktopWallpaperPosition.Fit);

        Assert.Equal(WallpaperPlacement.Default, placement);
    }

    [Fact]
    public void DesktopWallpaperInterop_RejectsUnknownWindowsPosition()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopWallpaperInterop.PositionToPlacement((DesktopWallpaperPosition)999));

        Assert.Equal("position", error.ParamName);
    }

    [Fact]
    public void DesktopWallpaperInterop_SetsWallpaperBeforePosition()
    {
        var desktopWallpaper = new RecordingDesktopWallpaperCom();

        DesktopWallpaperInterop.SetWallpaperThenPosition(
            desktopWallpaper,
            "DISPLAY-1",
            @"C:\Wallpapers\rendered.png",
            DesktopWallpaperPosition.Fill);

        Assert.Equal(
            [
                "SetWallpaper:DISPLAY-1:C:\\Wallpapers\\rendered.png",
                "SetPosition:Fill",
            ],
            desktopWallpaper.Calls);
    }

    [Fact]
    public void StaThreadRunner_RunsActionOnStaThreadFromMtaCaller()
    {
        ApartmentState? callerApartment = null;
        ApartmentState? actionApartment = null;

        var thread = new Thread(() =>
        {
            callerApartment = Thread.CurrentThread.GetApartmentState();
            StaThreadRunner.Run(() =>
            {
                actionApartment = Thread.CurrentThread.GetApartmentState();
            });
        });

        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        Assert.Equal(ApartmentState.MTA, callerApartment);
        Assert.Equal(ApartmentState.STA, actionApartment);
    }

    [Fact]
    public void StaThreadRunner_PropagatesExceptionFromStaThread()
    {
        Exception? observed = null;

        var thread = new Thread(() =>
        {
            observed = Record.Exception(() =>
                StaThreadRunner.Run(() => throw new InvalidOperationException("COM failed.")));
        });

        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        var error = Assert.IsType<InvalidOperationException>(observed);
        Assert.Equal("COM failed.", error.Message);
    }

    [Fact]
    public async Task DesktopWallpaperApplier_FailsBeforeInteropWhenRenderedFileIsMissing()
    {
        var applier = new DesktopWallpaperApplier();
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var wallpaper = new RenderedWallpaper(
            monitor,
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png"),
            1920,
            1080,
            DateTimeOffset.UtcNow);

        var result = await applier.ApplyAsync(wallpaper);

        Assert.False(result.Succeeded);
        Assert.Equal(ApplyErrorCodes.RenderedWallpaperMissing, result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void DesktopWallpaperApplier_RejectsNullWriter()
    {
        IDesktopWallpaperWriter? writer = null;

        var error = Assert.Throws<ArgumentNullException>(() => new DesktopWallpaperApplier(writer!));

        Assert.Equal("writer", error.ParamName);
    }

    [Fact]
    public async Task DesktopWallpaperApplier_RejectsNullWallpaper()
    {
        var applier = new DesktopWallpaperApplier(new RecordingDesktopWallpaperWriter());
        RenderedWallpaper? wallpaper = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => applier.ApplyAsync(wallpaper!));

        Assert.Equal("wallpaper", error.ParamName);
    }

    [Fact]
    public async Task DesktopWallpaperApplier_WritesRenderedWallpaperToMonitor()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-applier-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            var writer = new RecordingDesktopWallpaperWriter();
            var applier = new DesktopWallpaperApplier(writer);
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var wallpaper = new RenderedWallpaper(monitor, path, 1920, 1080, DateTimeOffset.UtcNow);

            var result = await applier.ApplyAsync(wallpaper);

            Assert.True(result.Succeeded);
            Assert.Equal("DISPLAY-1", writer.MonitorId);
            Assert.Equal(path, writer.WallpaperPath);
            Assert.Equal(DesktopWallpaperPosition.Fill, writer.Position);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task DesktopWallpaperApplier_MapsWriterFailureToFriendlyApplyFailure()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-applier-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            var applier = new DesktopWallpaperApplier(new ThrowingDesktopWallpaperWriter(
                new InvalidOperationException("COM unavailable.")));
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var wallpaper = new RenderedWallpaper(monitor, path, 1920, 1080, DateTimeOffset.UtcNow);

            var result = await applier.ApplyAsync(wallpaper);

            Assert.False(result.Succeeded);
            Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.ErrorCode);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task DesktopWallpaperApplier_PropagatesWriterCancellation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-applier-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            var applier = new DesktopWallpaperApplier(new ThrowingDesktopWallpaperWriter(
                new OperationCanceledException()));
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var wallpaper = new RenderedWallpaper(monitor, path, 1920, 1080, DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<OperationCanceledException>(() => applier.ApplyAsync(wallpaper));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task WindowsMonitorDetector_ReadsWallpaperSnapshotsThroughReader()
    {
        var detector = new WindowsMonitorDetector(new FixedDesktopWallpaperReader(
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center),
            WallpaperSource.FromSolidColor("#112233"),
            [
                new DesktopWallpaperSnapshot(
                    @"\\?\DISPLAY#ABC#1",
                    new MonitorBounds(10, 20, 1920, 1080),
                    @"C:\Wallpapers\one.jpg"),
            ]));

        var monitors = await detector.DetectAsync();

        var monitor = Assert.Single(monitors);
        Assert.Equal(@"\\?\DISPLAY#ABC#1", monitor.Identity.MonitorKey);
        Assert.Equal("ABC", monitor.Identity.DeviceName);
        Assert.Equal("Monitor 1 - ABC", monitor.DisplayName);
        Assert.Equal(1920, monitor.Identity.Width);
        Assert.Equal(1080, monitor.Identity.Height);
        Assert.Equal(10, monitor.Identity.X);
        Assert.Equal(20, monitor.Identity.Y);
        Assert.Equal(WallpaperSourceKind.Image, monitor.CurrentSource.Kind);
        Assert.Equal(@"C:\Wallpapers\one.jpg", monitor.CurrentSource.ImagePath);
        Assert.NotNull(monitor.CurrentPlacement);
        Assert.Equal(WallpaperFitMode.Contain, monitor.CurrentPlacement.FitMode);
    }

    [Fact]
    public async Task WindowsMonitorDetector_UsesBackgroundSourceWhenMonitorWallpaperIsEmpty()
    {
        var detector = new WindowsMonitorDetector(new FixedDesktopWallpaperReader(
            WallpaperPlacement.Default,
            WallpaperSource.FromSolidColor("#112233"),
            [
                new DesktopWallpaperSnapshot(
                    "DISPLAY-1",
                    new MonitorBounds(0, 0, 1920, 1080),
                    null),
            ]));

        var monitors = await detector.DetectAsync();

        var monitor = Assert.Single(monitors);
        Assert.Equal(WallpaperSourceKind.SolidColor, monitor.CurrentSource.Kind);
        Assert.Equal("#112233", monitor.CurrentSource.ColorHex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DesktopMonitorDisplayName_RejectsBlankMonitorId(string? monitorId)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() =>
            DesktopMonitorDisplayName.ShortenDeviceName(monitorId!));

        Assert.Equal("monitorId", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DesktopMonitorDisplayName_RejectsInvalidDisplayIndex(int displayIndex)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopMonitorDisplayName.Create(displayIndex, "DISPLAY-1"));

        Assert.Equal("displayIndex", error.ParamName);
    }

    [Fact]
    public void DesktopMonitorDisplayName_TruncatesLongDeviceIds()
    {
        var deviceId = new string('A', 60);

        var displayName = DesktopMonitorDisplayName.Create(2, deviceId);

        Assert.Equal($"Monitor 2 - {new string('A', 45)}...", displayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DesktopWallpaperSnapshot_RejectsBlankMonitorId(string? monitorId)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new DesktopWallpaperSnapshot(
            monitorId!,
            new MonitorBounds(0, 0, 1920, 1080),
            null));

        Assert.Equal("MonitorId", error.ParamName);
    }

    [Fact]
    public void DesktopWallpaperSnapshot_RejectsNullBounds()
    {
        MonitorBounds? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => new DesktopWallpaperSnapshot(
            "DISPLAY-1",
            bounds!,
            null));

        Assert.Equal("Bounds", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DesktopWallpaperSnapshot_WithExpressionRejectsBlankMonitorId(string? monitorId)
    {
        var snapshot = new DesktopWallpaperSnapshot(
            "DISPLAY-1",
            new MonitorBounds(0, 0, 1920, 1080),
            null);

        var error = Assert.ThrowsAny<ArgumentException>(() => snapshot with { MonitorId = monitorId! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void DesktopWallpaperSnapshot_WithExpressionRejectsNullBounds()
    {
        var snapshot = new DesktopWallpaperSnapshot(
            "DISPLAY-1",
            new MonitorBounds(0, 0, 1920, 1080),
            null);
        MonitorBounds? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => snapshot with { Bounds = bounds! });

        Assert.Equal("value", error.ParamName);
    }
}
