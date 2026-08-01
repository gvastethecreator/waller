using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Waller.Native.Core.Sessions;
using Waller.Native.Workflows.Apply;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class ApplyViewModel : ObservableObject
{
    private readonly ApplyWorkflow workflow;
    private readonly IShellWorkspace workspace;
    private readonly Func<LocalizedText> text;
    private readonly ApplyTextPresenter applyText;
    private readonly Action<string> setStatus;
    private readonly Action<bool> refreshSessionSurface;
    private readonly Action notifyWorkspaceStateChanged;
    private ApplyProgress? lastProgress;

    public ApplyViewModel(
        ApplyWorkflow workflow,
        IShellWorkspace workspace,
        Func<LocalizedText> text,
        Action<string> setStatus,
        Action<bool> refreshSessionSurface,
        Action notifyWorkspaceStateChanged)
    {
        this.workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.text = LocalizedTextSource.Require(text);
        this.setStatus = setStatus ?? throw new ArgumentNullException(nameof(setStatus));
        this.refreshSessionSurface = refreshSessionSurface ?? throw new ArgumentNullException(nameof(refreshSessionSurface));
        this.notifyWorkspaceStateChanged = notifyWorkspaceStateChanged
            ?? throw new ArgumentNullException(nameof(notifyWorkspaceStateChanged));
        applyText = new ApplyTextPresenter(this.text);
    }

    [ObservableProperty]
    public partial string ProgressText { get; set; } = string.Empty;

    public LocalizedText Text => text();

    public bool IsApplying => workflow.IsRunning;

    public bool CanStartApply => workspace.CanStartApply;

    public Visibility ProgressVisibility => VisibilityStates.When(IsApplying);

    public void RefreshLocalizedSurface()
    {
        OnPropertyChanged(nameof(Text));
        if (!IsApplying)
        {
            return;
        }

        ProgressText = lastProgress is null
            ? applyText.Preparing
            : applyText.Progress(lastProgress);
    }

    public void NotifyWorkspaceStateChanged()
    {
        OnPropertyChanged(nameof(CanStartApply));
    }

    [RelayCommand]
    private async Task ApplyAll()
    {
        if (CanStartApply)
        {
            await RunAsync(ApplyWorkflowRequest.AllReadySources());
        }
    }

    [RelayCommand]
    private async Task ApplyMonitor(MonitorRowViewModel? monitor)
    {
        if (CanStartApply && monitor is not null)
        {
            await RunAsync(ApplyWorkflowRequest.MonitorReadySource(monitor.MonitorKey));
        }
    }

    [RelayCommand]
    private void CancelApply()
    {
        if (workflow.Cancel())
        {
            lastProgress = null;
            ProgressText = applyText.Cancelled;
        }
    }

    private async Task<bool> RunAsync(ApplyWorkflowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lastProgress = null;
        ProgressText = applyText.Preparing;

        var run = workflow.RunAsync(request, progress =>
        {
            lastProgress = progress;
            ProgressText = applyText.Progress(progress);
        });
        NotifyApplyStateChanged();

        try
        {
            return PresentResult(await run);
        }
        finally
        {
            lastProgress = null;
            NotifyApplyStateChanged();
        }
    }

    private bool PresentResult(ApplyWorkflowResult outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Result is { } result)
        {
            workspace.ReplaceActiveSession(result.Session);
            refreshSessionSurface(false);
        }

        ProgressText = string.Empty;
        setStatus(outcome.Status switch
        {
            ApplyWorkflowStatus.Completed => applyText.Result(outcome.Result!),
            ApplyWorkflowStatus.Cancelled => applyText.Cancelled,
            ApplyWorkflowStatus.AlreadyRunning or ApplyWorkflowStatus.Unavailable => Text.CheckValue,
            ApplyWorkflowStatus.UnexpectedFailure => applyText.UnexpectedFailure,
            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome.Status,
                "Apply outcome is not supported."),
        });
        return outcome.Succeeded;
    }

    private void NotifyApplyStateChanged()
    {
        OnPropertyChanged(nameof(IsApplying));
        OnPropertyChanged(nameof(CanStartApply));
        OnPropertyChanged(nameof(ProgressVisibility));
        notifyWorkspaceStateChanged();
    }
}
