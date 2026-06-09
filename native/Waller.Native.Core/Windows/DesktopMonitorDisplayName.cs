namespace Waller.Native.Core.Windows;

internal static class DesktopMonitorDisplayName
{
    public static string Create(int displayIndex, string monitorId)
    {
        var deviceName = ShortenDeviceName(monitorId);
        return string.IsNullOrWhiteSpace(deviceName)
            ? $"Monitor {displayIndex}"
            : $"Monitor {displayIndex} - {deviceName}";
    }

    public static string ShortenDeviceName(string monitorId)
    {
        var trimmed = monitorId.Trim();
        var parts = trimmed.Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return parts[1];
        }

        return trimmed.Length <= 48 ? trimmed : $"{trimmed[..45]}...";
    }
}
