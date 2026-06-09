using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public static class DesktopWallpaperInterop
{
    public const string InteropMode = "Manual IDesktopWallpaper COM interop";

    private static readonly Guid DesktopWallpaperClassId = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

    public static WallpaperSource WallpaperPathToSource(string? wallpaperPath)
    {
        return WallpaperSourcePath.TryNormalizeImagePath(wallpaperPath, out var normalizedPath)
            ? WallpaperSource.FromImage(normalizedPath)
            : WallpaperSource.Empty;
    }

    internal static WallpaperSource BackgroundColorResultToSource(int hresult, uint colorRef)
    {
        if (hresult != 0)
        {
            return WallpaperSource.Empty;
        }

        var red = colorRef & 0xFF;
        var green = (colorRef >> 8) & 0xFF;
        var blue = (colorRef >> 16) & 0xFF;
        return WallpaperSource.FromSolidColor($"#{red:x2}{green:x2}{blue:x2}");
    }

    internal static WallpaperPlacement PositionToPlacement(DesktopWallpaperPosition position)
    {
        var fit = position switch
        {
            DesktopWallpaperPosition.Center => WallpaperFitMode.Center,
            DesktopWallpaperPosition.Tile => WallpaperFitMode.Tile,
            DesktopWallpaperPosition.Stretch => WallpaperFitMode.Stretch,
            DesktopWallpaperPosition.Fit => WallpaperFitMode.Contain,
            _ => WallpaperFitMode.Cover,
        };

        return new WallpaperPlacement(fit, WallpaperAnchor.Center);
    }

    internal static WallpaperPlacement PositionResultToPlacement(
        int hresult,
        DesktopWallpaperPosition position) =>
        hresult == 0 ? PositionToPlacement(position) : WallpaperPlacement.Default;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "IDesktopWallpaper is activated by CLSID as a COM object; Activator does not need a managed public parameterless constructor.")]
    internal static IDesktopWallpaper CreateDesktopWallpaper()
    {
        var type = Type.GetTypeFromCLSID(DesktopWallpaperClassId)
            ?? throw new InvalidOperationException("IDesktopWallpaper COM class is not registered.");

        return (IDesktopWallpaper)(Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("IDesktopWallpaper COM class could not be created."));
    }

    internal static string GetMonitorDevicePathAt(IDesktopWallpaper desktopWallpaper, uint monitorIndex)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.GetMonitorDevicePathAt(monitorIndex, out var monitorId));
        return StringFromCoTaskMem(monitorId);
    }

    internal static uint GetMonitorDevicePathCount(IDesktopWallpaper desktopWallpaper)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.GetMonitorDevicePathCount(out var count));
        return count;
    }

    internal static string? GetWallpaper(IDesktopWallpaper desktopWallpaper, string monitorId)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.GetWallpaper(monitorId, out var wallpaper));
        return wallpaper == IntPtr.Zero ? null : StringFromCoTaskMem(wallpaper);
    }

    internal static MonitorBounds GetMonitorBounds(IDesktopWallpaper desktopWallpaper, string monitorId)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.GetMonitorRECT(monitorId, out var rect));
        return new MonitorBounds(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top);
    }

    internal static DesktopWallpaperPosition GetPosition(IDesktopWallpaper desktopWallpaper)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.GetPosition(out var position));
        return position;
    }

    internal static WallpaperPlacement GetPositionPlacementOrDefault(IDesktopWallpaper desktopWallpaper)
    {
        var hresult = desktopWallpaper.GetPosition(out var position);
        return PositionResultToPlacement(hresult, position);
    }

    internal static WallpaperSource GetBackgroundColorSourceOrEmpty(IDesktopWallpaper desktopWallpaper)
    {
        var hresult = desktopWallpaper.GetBackgroundColor(out var color);
        return BackgroundColorResultToSource(hresult, color);
    }

    internal static void SetWallpaper(IDesktopWallpaper desktopWallpaper, string monitorId, string wallpaperPath)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.SetWallpaper(monitorId, wallpaperPath));
    }

    internal static void SetPosition(IDesktopWallpaper desktopWallpaper, DesktopWallpaperPosition position)
    {
        Marshal.ThrowExceptionForHR(desktopWallpaper.SetPosition(position));
    }

    internal static void SetWallpaperThenPosition(
        IDesktopWallpaper desktopWallpaper,
        string monitorId,
        string wallpaperPath,
        DesktopWallpaperPosition position)
    {
        SetWallpaper(desktopWallpaper, monitorId, wallpaperPath);
        SetPosition(desktopWallpaper, position);
    }

    private static string StringFromCoTaskMem(IntPtr pointer)
    {
        try
        {
            return Marshal.PtrToStringUni(pointer) ?? string.Empty;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pointer);
            }
        }
    }
}

[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDesktopWallpaper
{
    [PreserveSig]
    int SetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
        [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    [PreserveSig]
    int GetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
        out IntPtr wallpaper);

    [PreserveSig]
    int GetMonitorDevicePathAt(uint monitorIndex, out IntPtr monitorId);

    [PreserveSig]
    int GetMonitorDevicePathCount(out uint count);

    [PreserveSig]
    int GetMonitorRECT(
        [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
        out DesktopWallpaperRect displayRect);

    [PreserveSig]
    int SetBackgroundColor(uint color);

    [PreserveSig]
    int GetBackgroundColor(out uint color);

    [PreserveSig]
    int SetPosition(DesktopWallpaperPosition position);

    [PreserveSig]
    int GetPosition(out DesktopWallpaperPosition position);

    [PreserveSig]
    int SetSlideshow(IntPtr items);

    [PreserveSig]
    int GetSlideshow(out IntPtr items);

    [PreserveSig]
    int SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick);

    [PreserveSig]
    int GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick);

    [PreserveSig]
    int AdvanceSlideshow(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
        DesktopSlideshowDirection direction);

    [PreserveSig]
    int GetStatus(out DesktopSlideshowStatus state);

    [PreserveSig]
    int Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

[StructLayout(LayoutKind.Sequential)]
internal struct DesktopWallpaperRect
{
    public int Left;

    public int Top;

    public int Right;

    public int Bottom;
}

public enum DesktopWallpaperPosition
{
    Center = 0,
    Tile = 1,
    Stretch = 2,
    Fit = 3,
    Fill = 4,
    Span = 5,
}

[Flags]
internal enum DesktopSlideshowOptions
{
    ShuffleImages = 0x1,
}

internal enum DesktopSlideshowDirection
{
    Forward = 0,
    Backward = 1,
}

internal enum DesktopSlideshowStatus
{
    Enabled = 0x1,
    Slideshow = 0x2,
    DisabledByRemoteSession = 0x4,
}
