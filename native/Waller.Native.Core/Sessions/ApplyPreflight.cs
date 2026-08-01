using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public static class ApplyPreflight
{
    public static ApplyPreflightResult SkipMissingImageSources(ActiveSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var skippedKeys = session.Monitors
            .Where(monitor => WallpaperSourceFiles.IsMissingImageFile(monitor.DesiredAssignment.Source))
            .Select(monitor => monitor.Monitor.Identity.MonitorKey)
            .ToMonitorKeySet();

        var readyKeys = session.Monitors
            .Where(monitor => !MonitorKeys.Contains(skippedKeys, monitor.Monitor.Identity.MonitorKey))
            .Select(monitor => monitor.Monitor.Identity.MonitorKey)
            .ToMonitorKeySet();

        return MarkSkippedMonitors(session, readyKeys, skippedKeys);
    }

    public static ApplyPreflightResult SkipMissingImageSource(ActiveSession session, string monitorKey)
    {
        ArgumentNullException.ThrowIfNull(session);
        monitorKey = MonitorKeys.Require(monitorKey, nameof(monitorKey));

        var target = session.Monitors.FirstOrDefault(monitor =>
            MonitorKeys.Equals(monitor.Monitor.Identity.MonitorKey, monitorKey));
        if (target is null || !WallpaperSourceFiles.IsMissingImageFile(target.DesiredAssignment.Source))
        {
            return target is null
                ? ApplyPreflightResult.NoTargets(session)
                : ApplyPreflightResult.ReadyTarget(session, target.Monitor.Identity.MonitorKey);
        }

        return MarkSkippedMonitors(
            session,
            MonitorKeys.CreateSet(),
            MonitorKeys.CreateSet(target.Monitor.Identity.MonitorKey));
    }

    private static ApplyPreflightResult MarkSkippedMonitors(
        ActiveSession session,
        IReadOnlySet<string> readyKeys,
        IReadOnlySet<string> skippedKeys)
    {
        var result = ApplyPreflightResult.FromSets(session, readyKeys, skippedKeys);
        if (!result.HasSkippedMonitors)
        {
            return result;
        }

        var nextSession = session with
        {
            Monitors = session.Monitors.Select(monitor =>
                MonitorKeys.Contains(result.SkippedMonitorKeys, monitor.Monitor.Identity.MonitorKey)
                    ? monitor.WithApplyError(ApplyErrorCodes.MissingImageSource)
                    : monitor).ToList(),
        };

        return result.WithSession(nextSession);
    }

    private static HashSet<string> ToMonitorKeySet(this IEnumerable<string> monitorKeys) =>
        MonitorKeys.CreateSet(monitorKeys);
}
