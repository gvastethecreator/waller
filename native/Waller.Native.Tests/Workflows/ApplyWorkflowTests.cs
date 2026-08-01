using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Windows;
using Waller.Native.Workflows.Apply;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.Tests.Workflows;

public sealed class ApplyWorkflowTests
{
    [Fact]
    public async Task AllAndMonitorTargets_UseOneRequestInterface()
    {
        var allWorkspace = Workspace(CreateSession("DISPLAY-1", "DISPLAY-2"));
        var all = Workflow(allWorkspace, new SuccessfulApplier());
        var allResult = await all.RunAsync(ApplyWorkflowRequest.AllReadySources());

        var monitorWorkspace = Workspace(CreateSession("DISPLAY-1", "DISPLAY-2"));
        var monitor = Workflow(monitorWorkspace, new SuccessfulApplier());
        var monitorResult = await monitor.RunAsync(
            ApplyWorkflowRequest.MonitorReadySource("display-2"));

        Assert.Equal(2, allResult.Result?.Succeeded);
        Assert.Equal(1, monitorResult.Result?.Succeeded);
        Assert.Equal(MonitorApplyStatus.Clean, monitorResult.Result?.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Applied, monitorResult.Result?.Session.Monitors[1].ApplyStatus);
    }

    [Fact]
    public async Task EmptySession_CompletesAsNoOp()
    {
        var workspace = Workspace(ActiveSession.FromMonitors([]));
        var workflow = Workflow(workspace, new SuccessfulApplier());

        var result = await workflow.RunAsync(ApplyWorkflowRequest.AllReadySources());

        Assert.Equal(ApplyWorkflowStatus.Completed, result.Status);
        Assert.NotNull(result.Result);
        Assert.False(result.Result.HasAnyOutcome);
        Assert.False(workflow.IsRunning);
        Assert.False(workspace.IsApplyActive);
    }

    [Fact]
    public async Task PartialFailure_PreservesSuccessfulMonitor()
    {
        var workspace = Workspace(CreateSession("DISPLAY-1", "DISPLAY-2"));
        var workflow = Workflow(workspace, new FailingMonitorApplier("DISPLAY-2"));

        var result = await workflow.RunAsync(ApplyWorkflowRequest.AllReadySources());

        Assert.Equal(ApplyWorkflowStatus.Completed, result.Status);
        Assert.Equal(1, result.Result?.Succeeded);
        Assert.Equal(1, result.Result?.Failed);
        Assert.Equal(MonitorApplyStatus.Applied, result.Result?.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Error, result.Result?.Session.Monitors[1].ApplyStatus);
    }

    [Fact]
    public async Task Cancel_IsRequestedAndResourcesAreReleasedOnce()
    {
        var applier = new BlockingApplier(blockOnCall: 1);
        var workspace = Workspace(CreateSession("DISPLAY-1"));
        var workflow = Workflow(workspace, applier);
        var run = workflow.RunAsync(ApplyWorkflowRequest.AllReadySources());
        await applier.Blocked;

        Assert.True(workflow.Cancel());
        Assert.False(workflow.Cancel());
        var result = await run;

        Assert.Equal(ApplyWorkflowStatus.Cancelled, result.Status);
        Assert.False(workflow.IsRunning);
        Assert.False(workspace.IsApplyActive);
        Assert.False(workflow.Cancel());
    }

    [Fact]
    public async Task Cancel_PreservesPartialSuccess()
    {
        var applier = new BlockingApplier(blockOnCall: 2);
        var workspace = Workspace(CreateSession("DISPLAY-1", "DISPLAY-2"));
        var workflow = Workflow(workspace, applier);
        var run = workflow.RunAsync(ApplyWorkflowRequest.AllReadySources());
        await applier.Blocked;

        Assert.True(workflow.Cancel());
        var result = await run;

        Assert.Equal(ApplyWorkflowStatus.Cancelled, result.Status);
        Assert.Equal(1, result.Result?.Succeeded);
        Assert.Equal(MonitorApplyStatus.Applied, result.Result?.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Clean, result.Result?.Session.Monitors[1].ApplyStatus);
    }

    [Fact]
    public async Task ConcurrentRun_IsRejectedBeforeSecondAdapterCall()
    {
        var applier = new BlockingApplier(blockOnCall: 1);
        var workspace = Workspace(CreateSession("DISPLAY-1"));
        var workflow = Workflow(workspace, applier);
        var first = workflow.RunAsync(ApplyWorkflowRequest.AllReadySources());
        await applier.Blocked;

        var second = await workflow.RunAsync(ApplyWorkflowRequest.AllReadySources());

        Assert.Equal(ApplyWorkflowStatus.AlreadyRunning, second.Status);
        Assert.Equal(1, applier.CallCount);
        Assert.True(workflow.Cancel());
        await first;
    }

    [Fact]
    public async Task UnexpectedProgressFailure_ReturnsTechnicalOutcome()
    {
        var workspace = Workspace(CreateSession("DISPLAY-1"));
        var workflow = Workflow(workspace, new SuccessfulApplier());

        var result = await workflow.RunAsync(
            ApplyWorkflowRequest.AllReadySources(),
            _ => throw new InvalidOperationException("progress failed"));

        Assert.Equal(ApplyWorkflowStatus.UnexpectedFailure, result.Status);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.False(workflow.IsRunning);
        Assert.False(workspace.IsApplyActive);
    }

    private static ApplyWorkflow Workflow(ShellWorkspace workspace, IWallpaperApplier applier) =>
        new(new WallpaperApplyService(new PassthroughRenderer(), applier), workspace);

    private static ShellWorkspace Workspace(ActiveSession session) => new(session);

    private static ActiveSession CreateSession(params string[] monitorKeys) =>
        ActiveSession.FromMonitors(monitorKeys.Select((monitorKey, index) =>
            new MonitorSnapshot(
                new MonitorIdentity(
                    monitorKey,
                    monitorKey,
                    index + 1,
                    1920,
                    1080,
                    index * 1920,
                    0),
                $"Display {index + 1}",
                WallpaperSource.FromSolidColor("#112233"),
                WallpaperPlacement.Default)).ToList());

    private sealed class PassthroughRenderer : IWallpaperRenderer
    {
        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new RenderedWallpaper(
                request.Monitor.Identity,
                $@"C:\rendered\{request.Monitor.Identity.MonitorKey}.png",
                request.Monitor.Bounds.Width,
                request.Monitor.Bounds.Height,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class SuccessfulApplier : IWallpaperApplier
    {
        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplyResult.Success(wallpaper.Monitor));
    }

    private sealed class FailingMonitorApplier(string monitorKey) : IWallpaperApplier
    {
        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MonitorKeys.Equals(wallpaper.Monitor.MonitorKey, monitorKey)
                ? ApplyResult.Failure(wallpaper.Monitor, ApplyErrorCodes.WallpaperApplyFailed)
                : ApplyResult.Success(wallpaper.Monitor));
    }

    private sealed class BlockingApplier(int blockOnCall) : IWallpaperApplier
    {
        private readonly TaskCompletionSource blocked =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int callCount;

        public Task Blocked => blocked.Task;

        public int CallCount => Volatile.Read(ref callCount);

        public async Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            var currentCall = Interlocked.Increment(ref callCount);
            if (currentCall == blockOnCall)
            {
                blocked.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return ApplyResult.Success(wallpaper.Monitor);
        }
    }
}
