using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

internal sealed class ApplyRunTracker(int total, ApplyProgressHandler? progress)
{
    private int completed;

    public int Succeeded { get; private set; }

    public int Failed { get; private set; }

    public void ReportStarting(MonitorSession monitor)
    {
        progress?.Invoke(new ApplyProgress(
            completed,
            total,
            monitor.Monitor.DisplayName,
            MonitorApplyStatus.Applying));
    }

    public void RecordSuccess()
    {
        Succeeded++;
        completed++;
    }

    public void RecordFailure()
    {
        Failed++;
        completed++;
    }

    public void Record(MonitorApplyStepResult result)
    {
        if (result.Succeeded)
        {
            RecordSuccess();
            return;
        }

        RecordFailure();
    }

    public void ReportCompleted(MonitorSession monitor)
    {
        progress?.Invoke(new ApplyProgress(
            completed,
            total,
            monitor.Monitor.DisplayName,
            monitor.ApplyStatus));
    }

    public ApplySessionResult ToResult(ActiveSession session, IReadOnlyList<MonitorSession> monitors)
    {
        return new ApplySessionResult(
            session with { Monitors = monitors.ToList() },
            Succeeded,
            Failed);
    }

    public ApplyCanceledException ToCanceledException(
        ActiveSession session,
        IReadOnlyList<MonitorSession> monitors)
    {
        return new ApplyCanceledException(ToResult(session, monitors));
    }
}
