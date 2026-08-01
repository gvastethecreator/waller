using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public sealed record ApplyPreflightResult
{
    public ApplyPreflightResult(
        ActiveSession session,
        IReadOnlySet<string> readyMonitorKeys,
        IReadOnlySet<string> skippedMonitorKeys)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(readyMonitorKeys);
        ArgumentNullException.ThrowIfNull(skippedMonitorKeys);

        var ready = MonitorKeys.CreateSet(readyMonitorKeys);
        var skipped = MonitorKeys.CreateSet(skippedMonitorKeys);
        EnsureDisjoint(ready, skipped);

        Session = session;
        ReadyMonitorKeys = ready;
        SkippedMonitorKeys = skipped;
    }

    public ActiveSession Session { get; }

    public IReadOnlySet<string> ReadyMonitorKeys { get; }

    public IReadOnlySet<string> SkippedMonitorKeys { get; }

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

    public ApplyPreflightResult WithSession(ActiveSession session) =>
        new(session, ReadyMonitorKeys, SkippedMonitorKeys);

    private static void EnsureDisjoint(
        IReadOnlySet<string> readyMonitorKeys,
        IReadOnlySet<string> skippedMonitorKeys)
    {
        foreach (var readyMonitorKey in readyMonitorKeys)
        {
            if (MonitorKeys.Contains(skippedMonitorKeys, readyMonitorKey))
            {
                throw new ArgumentException(
                    "Apply preflight cannot mark a monitor as both ready and skipped.",
                    nameof(skippedMonitorKeys));
            }
        }
    }
}
