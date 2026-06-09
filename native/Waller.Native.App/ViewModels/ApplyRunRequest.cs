using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal static class ApplyRunRequest
{
    public static Func<ApplyProgressHandler?, CancellationToken, Task<ApplySessionResult>> AllReadySources(
        WallpaperApplyService applyService,
        ActiveSession session) =>
        (progress, cancellationToken) => applyService.ApplyAllReadySourcesAsync(
            session,
            progress,
            cancellationToken);

    public static Func<ApplyProgressHandler?, CancellationToken, Task<ApplySessionResult>> MonitorReadySource(
        WallpaperApplyService applyService,
        ActiveSession session,
        MonitorRowViewModel monitor) =>
        (progress, cancellationToken) => applyService.ApplyMonitorReadySourceAsync(
            session,
            monitor.MonitorKey,
            progress,
            cancellationToken);
}
