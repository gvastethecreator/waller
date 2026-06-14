using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Windows;

namespace Waller.Native.Core.Sessions;

public sealed class WallpaperApplyService
{
    private readonly IWallpaperRenderer renderer;
    private readonly IWallpaperApplier applier;

    public WallpaperApplyService(IWallpaperRenderer renderer, IWallpaperApplier applier)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(applier);

        this.renderer = renderer;
        this.applier = applier;
    }

    public async Task<ApplySessionResult> ApplyMonitorAsync(
        ActiveSession session,
        string monitorKey,
        ApplyProgressHandler? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyAsync(
            session,
            ApplyTargetPlan.Monitor(monitorKey),
            progress,
            cancellationToken);
    }

    public async Task<ApplySessionResult> ApplyMonitorReadySourceAsync(
        ActiveSession session,
        string monitorKey,
        ApplyProgressHandler? progress = null,
        CancellationToken cancellationToken = default)
    {
        var preflight = ApplyPreflight.SkipMissingImageSource(session, monitorKey);
        return await ApplyReadyPreflightAsync(preflight, progress, cancellationToken);
    }

    public async Task<ApplySessionResult> ApplyAllAsync(
        ActiveSession session,
        ApplyProgressHandler? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyAsync(session, ApplyTargetPlan.All, progress, cancellationToken);
    }

    public async Task<ApplySessionResult> ApplyAllReadySourcesAsync(
        ActiveSession session,
        ApplyProgressHandler? progress = null,
        CancellationToken cancellationToken = default)
    {
        var preflight = ApplyPreflight.SkipMissingImageSources(session);
        return await ApplyReadyPreflightAsync(preflight, progress, cancellationToken);
    }

    private async Task<ApplySessionResult> ApplyReadyPreflightAsync(
        ApplyPreflightResult preflight,
        ApplyProgressHandler? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preflight);

        if (!preflight.HasReadyMonitors)
        {
            return ApplySessionResult.SkippedOnly(preflight.Session, preflight.SkippedCount);
        }

        try
        {
            var result = await ApplyAsync(
                preflight.Session,
                ApplyTargetPlan.ReadyKeys(preflight.ReadyMonitorKeys),
                progress,
                cancellationToken);

            return result.WithSkipped(preflight.SkippedCount);
        }
        catch (ApplyCanceledException error)
        {
            throw error.WithSkipped(preflight.SkippedCount);
        }
    }

    public async Task<ApplySessionResult> ApplyMatchingAsync(
        ActiveSession session,
        Func<MonitorSession, bool> shouldApply,
        ApplyProgressHandler? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyAsync(session, ApplyTargetPlan.Matching(shouldApply), progress, cancellationToken);
    }

    private async Task<ApplySessionResult> ApplyAsync(
        ActiveSession session,
        ApplyTargetPlan targetPlan,
        ApplyProgressHandler? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(targetPlan);

        var monitors = session.Monitors.ToList();
        var total = targetPlan.Count(monitors);
        var tracker = new ApplyRunTracker(total, progress);

        for (var index = 0; index < monitors.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw tracker.ToCanceledException(session, monitors);
            }

            var monitor = monitors[index];
            if (!targetPlan.Includes(monitor))
            {
                continue;
            }

            tracker.ReportStarting(monitor);
            monitors[index] = monitor.WithApplying();

            try
            {
                var result = await ApplyMonitorStepAsync(monitor, cancellationToken);
                tracker.Record(result);
                monitors[index] = result.Monitor;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                monitors[index] = monitor;
                throw tracker.ToCanceledException(session, monitors);
            }
            catch (Exception error)
            {
                tracker.RecordFailure();
                monitors[index] = monitor.WithApplyError(
                    ApplyErrorClassifier.FriendlyErrorCode(error),
                    error.Message);
            }

            tracker.ReportCompleted(monitors[index]);
        }

        return tracker.ToResult(session, monitors);
    }

    private async Task<MonitorApplyStepResult> ApplyMonitorStepAsync(
        MonitorSession monitor,
        CancellationToken cancellationToken)
    {
        try
        {
            var rendered = await renderer.RenderAsync(
                new RenderRequest(monitor.Monitor, monitor.DesiredAssignment),
                cancellationToken);
            var result = await applier.ApplyAsync(rendered, cancellationToken);

            return result.Succeeded
                ? MonitorApplyStepResult.Success(monitor)
                : MonitorApplyStepResult.Failure(
                    monitor,
                    ApplyErrorClassifier.FriendlyErrorCode(result.ErrorCode),
                    result.ErrorMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            return MonitorApplyStepResult.Failure(
                monitor,
                ApplyErrorClassifier.FriendlyErrorCode(error),
                error.Message);
        }
    }
}
