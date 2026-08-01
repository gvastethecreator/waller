using Waller.Native.Core.Sessions;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.Workflows.Apply;

public sealed class ApplyWorkflow
{
    private readonly object gate = new();
    private readonly WallpaperApplyService service;
    private readonly IShellWorkspace workspace;
    private ApplyLease? activeLease;
    private bool cancellationRequested;

    public ApplyWorkflow(WallpaperApplyService service, IShellWorkspace workspace)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public bool IsRunning
    {
        get
        {
            lock (gate)
            {
                return activeLease is not null;
            }
        }
    }

    public async Task<ApplyWorkflowResult> RunAsync(
        ApplyWorkflowRequest request,
        ApplyProgressHandler? progress = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lease = TryBeginRun(out var blockedResult);
        if (lease is null)
        {
            return blockedResult!;
        }

        try
        {
            var session = workspace.ActiveSession;
            var result = request.Target switch
            {
                ApplyWorkflowTarget.AllReadySources =>
                    await service.ApplyAllReadySourcesAsync(session, progress, lease.Token),
                ApplyWorkflowTarget.MonitorReadySource =>
                    await service.ApplyMonitorReadySourceAsync(session, request.MonitorKey!, progress, lease.Token),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Target,
                    "Apply target is not supported."),
            };

            return ApplyWorkflowResult.Completed(result);
        }
        catch (ApplyCanceledException error)
        {
            return ApplyWorkflowResult.Cancelled(error.Result);
        }
        catch (OperationCanceledException)
        {
            return ApplyWorkflowResult.Cancelled();
        }
        catch (Exception error)
        {
            return ApplyWorkflowResult.UnexpectedFailure(error);
        }
        finally
        {
            EndRun(lease);
        }
    }

    public bool Cancel()
    {
        lock (gate)
        {
            if (activeLease is null || cancellationRequested)
            {
                return false;
            }

            cancellationRequested = true;
            activeLease.Cancel();
            return true;
        }
    }

    private ApplyLease? TryBeginRun(out ApplyWorkflowResult? blockedResult)
    {
        lock (gate)
        {
            if (activeLease is not null || workspace.IsApplyActive)
            {
                blockedResult = ApplyWorkflowResult.AlreadyRunning();
                return null;
            }

            if (!workspace.CanStartApply)
            {
                blockedResult = ApplyWorkflowResult.Unavailable();
                return null;
            }

            try
            {
                activeLease = workspace.BeginApply();
                cancellationRequested = false;
                blockedResult = null;
                return activeLease;
            }
            catch (InvalidOperationException)
            {
                blockedResult = workspace.IsApplyActive
                    ? ApplyWorkflowResult.AlreadyRunning()
                    : ApplyWorkflowResult.Unavailable();
                return null;
            }
        }
    }

    private void EndRun(ApplyLease lease)
    {
        lock (gate)
        {
            if (!ReferenceEquals(activeLease, lease))
            {
                throw new InvalidOperationException("The completed Apply lease is not active.");
            }

            activeLease = null;
            cancellationRequested = false;
        }

        lease.Dispose();
    }
}
