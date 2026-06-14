using CommunityToolkit.Mvvm.Input;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private async Task ApplyAll()
    {
        if (!CanStartApply)
        {
            return;
        }

        await RunApplyAsync(ApplyRunRequest.AllReadySources(applyService, activeSession));
    }

    [RelayCommand]
    private async Task ApplyMonitor(MonitorRowViewModel? monitor)
    {
        if (!CanStartApply || monitor is null)
        {
            return;
        }

        await RunApplyAsync(ApplyRunRequest.MonitorReadySource(applyService, activeSession, monitor));
    }

    [RelayCommand]
    private void CancelApply()
    {
        if (!IsApplying)
        {
            return;
        }

        applyRunState.Cancel();
        ApplyProgressText = applyText.Cancelled;
    }

    private async Task<bool> RunApplyAsync(
        Func<ApplyProgressHandler, CancellationToken, Task<ApplySessionResult>> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);

        if (IsApplying)
        {
            return false;
        }

        var cancellationToken = BeginApplyRun();
        try
        {
            var result = await apply(progress =>
            {
                ApplyProgressText = applyText.Progress(progress);
            }, cancellationToken);

            return PresentApplyRunUiState(ApplyRunUiState.Success(result, applyText));
        }
        catch (Exception error)
        {
            return PresentApplyRunUiState(ApplyRunUiState.FromException(error, applyText));
        }
        finally
        {
            EndApplyRun();
        }
    }

    private bool PresentApplyRunUiState(ApplyRunUiState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Session is not null)
        {
            activeSession = state.Session;
            RefreshSessionSurface(selectFirst: false);
        }

        ApplyProgressText = state.ProgressText;
        StatusText = state.StatusText;
        return state.Succeeded;
    }

    private CancellationToken BeginApplyRun()
    {
        var cancellationToken = applyRunState.Begin();
        IsApplying = true;
        ApplyProgressText = applyText.Preparing;
        return cancellationToken;
    }

    private void EndApplyRun()
    {
        applyRunState.End();
        IsApplying = false;
    }
}
