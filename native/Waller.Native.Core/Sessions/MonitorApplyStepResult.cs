using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

internal sealed record MonitorApplyStepResult(
    MonitorSession Monitor,
    bool Succeeded)
{
    public static MonitorApplyStepResult Success(MonitorSession monitor) =>
        new(monitor.WithAppliedAssignment(), Succeeded: true);

    public static MonitorApplyStepResult Failure(MonitorSession monitor, string errorCode) =>
        new(monitor.WithApplyError(errorCode), Succeeded: false);
}
