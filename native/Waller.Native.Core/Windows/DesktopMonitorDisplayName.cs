using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

internal static class DesktopMonitorDisplayName
{
    public static string Create(int displayIndex, string monitorId)
    {
        if (displayIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayIndex), "Display index must be positive.");
        }

        var deviceName = ShortenDeviceName(monitorId);
        return string.IsNullOrWhiteSpace(deviceName)
            ? $"Monitor {displayIndex}"
            : $"Monitor {displayIndex} - {deviceName}";
    }

    public static string ShortenDeviceName(string monitorId)
    {
        var trimmed = MonitorKeys.Require(monitorId, nameof(monitorId)).Trim();
        var parts = trimmed.Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return parts[1];
        }

        return trimmed.Length <= 48 ? trimmed : $"{trimmed[..45]}...";
    }
}
