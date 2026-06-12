using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private void ForgetMissingMonitor(MissingMonitorRowViewModel? monitor)
    {
        if (!CanEditMonitorAssignment || monitor is null)
        {
            return;
        }

        ApplyDisconnectedMonitorEdit(
            DisconnectedMonitorEdit.Forget(sessionEditor, activeSession, monitor, monitorEditText));
    }

    [RelayCommand]
    private void ReassignMissingMonitor(MissingMonitorRowViewModel? monitor)
    {
        if (!CanEditMonitorAssignment || monitor is null)
        {
            return;
        }

        ApplyDisconnectedMonitorEdit(
            DisconnectedMonitorEdit.Reassign(sessionEditor, activeSession, monitor, SelectedMonitor, monitorEditText));
    }

    private void ApplyDisconnectedMonitorEdit(DisconnectedMonitorEditResult result)
    {
        if (result.Session is not null)
        {
            activeSession = result.Session;
            RefreshSessionSurface(selectFirst: false);
        }

        StatusText = result.StatusText;
    }
}
