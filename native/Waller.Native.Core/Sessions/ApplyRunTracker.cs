using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

internal sealed class ApplyRunTracker
{
    private readonly int total;
    private readonly ApplyProgressHandler? progress;
    private int completed;

    public ApplyRunTracker(int total, ApplyProgressHandler? progress)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);

        this.total = total;
        this.progress = progress;
    }

    public int Succeeded { get; private set; }

    public int Failed { get; private set; }

    public void ReportStarting(MonitorSession monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        progress?.Invoke(new ApplyProgress(
            completed,
            total,
            monitor.Monitor.DisplayName,
            MonitorApplyStatus.Applying));
    }

    public void RecordSuccess()
    {
        RecordCompletedStep();
        Succeeded++;
    }

    public void RecordFailure()
    {
        RecordCompletedStep();
        Failed++;
    }

    public void Record(MonitorApplyStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Succeeded)
        {
            RecordSuccess();
            return;
        }

        RecordFailure();
    }

    public void ReportCompleted(MonitorSession monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        progress?.Invoke(new ApplyProgress(
            completed,
            total,
            monitor.Monitor.DisplayName,
            monitor.ApplyStatus));
    }

    public ApplySessionResult ToResult(ActiveSession session, IReadOnlyList<MonitorSession> monitors)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(monitors);

        return new ApplySessionResult(
            session with { Monitors = monitors.ToList() },
            Succeeded,
            Failed);
    }

    public ApplyCanceledException ToCanceledException(
        ActiveSession session,
        IReadOnlyList<MonitorSession> monitors)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(monitors);

        return new ApplyCanceledException(ToResult(session, monitors));
    }

    private void RecordCompletedStep()
    {
        if (completed >= total)
        {
            throw new InvalidOperationException("Apply tracker cannot record more completed steps than its total.");
        }

        completed++;
    }
}
