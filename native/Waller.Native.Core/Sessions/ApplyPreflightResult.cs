using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public sealed record ApplyPreflightResult(
    ActiveSession Session,
    IReadOnlySet<string> ReadyMonitorKeys,
    IReadOnlySet<string> SkippedMonitorKeys)
{
    public static ApplyPreflightResult NoTargets(ActiveSession session) =>
        new(session, MonitorKeys.CreateSet(), MonitorKeys.CreateSet());

    public static ApplyPreflightResult ReadyTarget(ActiveSession session, string monitorKey) =>
        new(session, MonitorKeys.CreateSet(monitorKey), MonitorKeys.CreateSet());

    public static ApplyPreflightResult FromSets(
        ActiveSession session,
        IEnumerable<string> readyMonitorKeys,
        IEnumerable<string> skippedMonitorKeys) =>
        new(
            session,
            MonitorKeys.CreateSet(readyMonitorKeys),
            MonitorKeys.CreateSet(skippedMonitorKeys));

    public bool HasReadyMonitors => ReadyMonitorKeys.Count > 0;

    public bool HasSkippedMonitors => SkippedMonitorKeys.Count > 0;

    public int SkippedCount => SkippedMonitorKeys.Count;
}
