using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

internal sealed class ApplyTargetPlan
{
    private readonly Func<MonitorSession, bool> shouldApply;

    private ApplyTargetPlan(Func<MonitorSession, bool> shouldApply)
    {
        ArgumentNullException.ThrowIfNull(shouldApply);

        this.shouldApply = shouldApply;
    }

    public static ApplyTargetPlan All { get; } = new(_ => true);

    public static ApplyTargetPlan None { get; } = new(_ => false);

    public static ApplyTargetPlan Monitor(string monitorKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monitorKey);

        return new ApplyTargetPlan(
            monitor => MonitorKeys.Equals(monitor.Monitor.Identity.MonitorKey, monitorKey));
    }

    public static ApplyTargetPlan ReadyKeys(IReadOnlySet<string> monitorKeys)
    {
        ArgumentNullException.ThrowIfNull(monitorKeys);

        var keys = MonitorKeys.CreateSet(monitorKeys);
        if (keys.Count == 0)
        {
            return None;
        }

        return new ApplyTargetPlan(
            monitor => keys.Contains(monitor.Monitor.Identity.MonitorKey));
    }

    public static ApplyTargetPlan Matching(Func<MonitorSession, bool> shouldApply)
    {
        ArgumentNullException.ThrowIfNull(shouldApply);

        return new ApplyTargetPlan(shouldApply);
    }

    public bool Includes(MonitorSession monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return shouldApply(monitor);
    }

    public int Count(IReadOnlyList<MonitorSession> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        return monitors.Count(Includes);
    }
}
