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

    private static Preset CreatePreset(IReadOnlyList<PresetAssignment> assignments)
    {
        var now = DateTimeOffset.UtcNow;
        return new Preset(Preset.CurrentSchemaVersion, Guid.NewGuid(), "Desk", assignments, now, now);
    }

    private static MonitorSnapshot CreateMonitor(
        string key,
        int width,
        int height,
        WallpaperSource source)
    {
        var identity = new MonitorIdentity(key, key, 1, width, height, 0, 0);
        return new MonitorSnapshot(identity, "Monitor 1", source);
    }

    private static (int Width, int Height) ReadPngSize(string path)
    {
        using var file = File.OpenRead(path);
        var header = new byte[24];
        var read = file.Read(header, 0, header.Length);
        Assert.Equal(header.Length, read);
        Assert.Equal(137, header[0]);
        Assert.Equal((byte)'P', header[1]);
        Assert.Equal((byte)'N', header[2]);
        Assert.Equal((byte)'G', header[3]);

        return (ReadBigEndian(header, 16), ReadBigEndian(header, 20));
    }

    private static async Task WriteTwoColorSourceAsync(string path)
    {
        var pixels = new PixelBuffer(2, 1, new byte[6]);
        pixels.SetPixel(0, 0, new RgbColor(255, 0, 0));
        pixels.SetPixel(1, 0, new RgbColor(0, 255, 0));
        await SolidColorPngWriter.WriteAsync(path, pixels);
    }

    private static async Task WriteFourColorSourceAsync(string path)
    {
        var pixels = new PixelBuffer(4, 1, new byte[12]);
        pixels.SetPixel(0, 0, new RgbColor(255, 0, 0));
        pixels.SetPixel(1, 0, new RgbColor(0, 255, 0));
        pixels.SetPixel(2, 0, new RgbColor(0, 0, 255));
        pixels.SetPixel(3, 0, new RgbColor(255, 255, 255));
        await SolidColorPngWriter.WriteAsync(path, pixels);
    }

    private static PixelBuffer ReadPngPixels(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(137, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);

        var width = ReadBigEndian(bytes, 16);
        var height = ReadBigEndian(bytes, 20);
        var idatOffset = FindChunk(bytes, "IDAT");
        var idatLength = ReadBigEndian(bytes, idatOffset - 4);
        var idat = bytes[(idatOffset + 4)..(idatOffset + 4 + idatLength)];
        var raw = InflateStoredZlib(idat);
        var pixels = new PixelBuffer(width, height, new byte[checked(width * height * 3)]);
        var rowLength = 1 + (width * 3);

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * rowLength;
            Assert.Equal(0, raw[rowOffset]);
            Buffer.BlockCopy(raw, rowOffset + 1, pixels.Data, y * width * 3, width * 3);
        }

        return pixels;
    }

    private static int FindChunk(byte[] bytes, string chunkName)
    {
        var chunkBytes = System.Text.Encoding.ASCII.GetBytes(chunkName);
        var offset = 8;
        while (offset < bytes.Length)
        {
            var length = ReadBigEndian(bytes, offset);
            var typeOffset = offset + 4;
            if (bytes.AsSpan(typeOffset, 4).SequenceEqual(chunkBytes))
            {
                return typeOffset;
            }

            offset += 12 + length;
        }

        throw new InvalidOperationException($"PNG chunk not found: {chunkName}");
    }

    private static byte[] InflateStoredZlib(byte[] idat)
    {
        Assert.Equal(0x78, idat[0]);
        var output = new List<byte>();
        var offset = 2;
        var final = false;

        while (!final)
        {
            var header = idat[offset++];
            final = (header & 0x01) == 1;
            Assert.Equal(0, header & 0x06);

            var length = idat[offset] | (idat[offset + 1] << 8);
            offset += 2;
            var complement = idat[offset] | (idat[offset + 1] << 8);
            offset += 2;
            Assert.Equal((ushort)~length, (ushort)complement);

            output.AddRange(idat.AsSpan(offset, length).ToArray());
            offset += length;
        }

        return output.ToArray();
    }

    private static int ReadBigEndian(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24)
            | (buffer[offset + 1] << 16)
            | (buffer[offset + 2] << 8)
            | buffer[offset + 3];
    }

    private static WallpaperApplyService CreateApplyService(
        string root,
        RecordingWallpaperApplier applier)
    {
        return new WallpaperApplyService(
            new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root)),
            applier);
    }

    private sealed class RecordingWallpaperApplier(
        bool succeed,
        string errorCode = ApplyErrorCodes.WallpaperApplyFailed,
        string errorMessage = "Fake failure.") : IWallpaperApplier
    {
        public RenderedWallpaper? LastWallpaper { get; private set; }

        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            LastWallpaper = wallpaper;
            return Task.FromResult(succeed
                ? ApplyResult.Success(wallpaper.Monitor)
                : ApplyResult.Failure(wallpaper.Monitor, errorCode, errorMessage));
        }
    }

    private sealed class FailingMonitorWallpaperApplier(string failedMonitorKey) : IWallpaperApplier
    {
        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            var result = string.Equals(
                wallpaper.Monitor.MonitorKey,
                failedMonitorKey,
                StringComparison.OrdinalIgnoreCase)
                ? ApplyResult.Failure(wallpaper.Monitor, ApplyErrorCodes.WallpaperApplyFailed)
                : ApplyResult.Success(wallpaper.Monitor);

            return Task.FromResult(result);
        }
    }

    private sealed class CancelingWallpaperRenderer : IWallpaperRenderer
    {
        public CancellationTokenSource? Cancellation { get; set; }

        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            Cancellation?.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class ThrowingWallpaperRenderer(Exception error) : IWallpaperRenderer
    {
        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            throw error;
        }
    }

    private sealed class PassthroughWallpaperRenderer : IWallpaperRenderer
    {
        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new RenderedWallpaper(
                request.Monitor.Identity,
                $@"C:\rendered\{request.Monitor.Identity.MonitorKey}.png",
                request.Monitor.Bounds.Width,
                request.Monitor.Bounds.Height,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class CancelOnSecondWallpaperApplier : IWallpaperApplier
    {
        private int count;

        public CancellationTokenSource? Cancellation { get; set; }

        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            count++;
            if (count == 2)
            {
                Cancellation?.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(ApplyResult.Success(wallpaper.Monitor));
        }
    }

    private sealed class RecordingDesktopWallpaperWriter : IDesktopWallpaperWriter
    {
        public string? MonitorId { get; private set; }

        public string? WallpaperPath { get; private set; }

        public DesktopWallpaperPosition? Position { get; private set; }

        public void SetWallpaper(
            string monitorId,
            string wallpaperPath,
            DesktopWallpaperPosition position)
        {
            MonitorId = monitorId;
            WallpaperPath = wallpaperPath;
            Position = position;
        }
    }

    private sealed class RecordingDesktopWallpaperCom : IDesktopWallpaper
    {
        public List<string> Calls { get; } = [];

        public int SetWallpaper(string? monitorId, string wallpaper)
        {
            Calls.Add($"SetWallpaper:{monitorId}:{wallpaper}");
            return 0;
        }

        public int GetWallpaper(string? monitorId, out IntPtr wallpaper)
        {
            wallpaper = IntPtr.Zero;
            return 0;
        }

        public int GetMonitorDevicePathAt(uint monitorIndex, out IntPtr monitorId)
        {
            monitorId = IntPtr.Zero;
            return 0;
        }

        public int GetMonitorDevicePathCount(out uint count)
        {
            count = 0;
            return 0;
        }

        public int GetMonitorRECT(string monitorId, out DesktopWallpaperRect displayRect)
        {
            displayRect = default;
            return 0;
        }

        public int SetBackgroundColor(uint color) => 0;

        public int GetBackgroundColor(out uint color)
        {
            color = 0;
            return 0;
        }

        public int SetPosition(DesktopWallpaperPosition position)
        {
            Calls.Add($"SetPosition:{position}");
            return 0;
        }

        public int GetPosition(out DesktopWallpaperPosition position)
        {
            position = DesktopWallpaperPosition.Fill;
            return 0;
        }

        public int SetSlideshow(IntPtr items) => 0;

        public int GetSlideshow(out IntPtr items)
        {
            items = IntPtr.Zero;
            return 0;
        }

        public int SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) => 0;

        public int GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick)
        {
            options = default;
            slideshowTick = 0;
            return 0;
        }

        public int AdvanceSlideshow(string? monitorId, DesktopSlideshowDirection direction) => 0;

        public int GetStatus(out DesktopSlideshowStatus state)
        {
            state = default;
            return 0;
        }

        public int Enable(bool enable) => 0;
    }

    private sealed class ThrowingDesktopWallpaperWriter(Exception error) : IDesktopWallpaperWriter
    {
        public void SetWallpaper(
            string monitorId,
            string wallpaperPath,
            DesktopWallpaperPosition position)
        {
            throw error;
        }
    }

    private sealed class FixedDesktopWallpaperReader(
        WallpaperPlacement currentPlacement,
        WallpaperSource backgroundSource,
        IReadOnlyList<DesktopWallpaperSnapshot> monitors) : IDesktopWallpaperReader
    {
        public WallpaperPlacement CurrentPlacement => currentPlacement;

        public WallpaperSource BackgroundSource => backgroundSource;

        public IReadOnlyList<DesktopWallpaperSnapshot> ReadMonitors(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return monitors;
        }
    }

    private sealed class FixedMonitorDetector(IReadOnlyList<MonitorSnapshot> monitors) : IMonitorDetector
    {
        public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(monitors);
        }
    }

    private sealed class ThrowingMonitorDetector(Exception error) : IMonitorDetector
    {
        public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
        {
            throw error;
        }
    }
}
