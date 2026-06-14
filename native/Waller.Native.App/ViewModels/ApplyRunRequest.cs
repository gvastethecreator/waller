using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal static class ApplyRunRequest
{
    public static Func<ApplyProgressHandler?, CancellationToken, Task<ApplySessionResult>> AllReadySources(
        WallpaperApplyService applyService,
        ActiveSession session)
    {
        ArgumentNullException.ThrowIfNull(applyService);
        ArgumentNullException.ThrowIfNull(session);

        return (progress, cancellationToken) => applyService.ApplyAllReadySourcesAsync(
            session,
            progress,
            cancellationToken);
    }

    public static Func<ApplyProgressHandler?, CancellationToken, Task<ApplySessionResult>> MonitorReadySource(
        WallpaperApplyService applyService,
        ActiveSession session,
        MonitorRowViewModel monitor)
    {
        ArgumentNullException.ThrowIfNull(applyService);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(monitor);
        var monitorKey = MonitorKeys.Require(monitor.MonitorKey, "monitor.MonitorKey");

        return (progress, cancellationToken) => applyService.ApplyMonitorReadySourceAsync(
            session,
            monitorKey,
            progress,
            cancellationToken);
    }
}
