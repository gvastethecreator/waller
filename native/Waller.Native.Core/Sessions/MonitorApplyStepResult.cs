using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

internal sealed record MonitorApplyStepResult
{
    private MonitorApplyStepResult(MonitorSession monitor, bool Succeeded)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        Monitor = monitor;
        this.Succeeded = Succeeded;
    }

    public MonitorSession Monitor { get; }

    public bool Succeeded { get; }

    public static MonitorApplyStepResult Success(MonitorSession monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return new(monitor.WithAppliedAssignment(), Succeeded: true);
    }

    public static MonitorApplyStepResult Failure(
        MonitorSession monitor,
        string? errorCode,
        string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return new(
            monitor.WithApplyError(ApplyErrorCodes.Normalize(errorCode), errorMessage),
            Succeeded: false);
    }
}
