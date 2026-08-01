using CommunityToolkit.Mvvm.Input;
using Waller.Native.Core.Sessions;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadCurrentSessionAsync(shellText.LoadedCurrentSetup);
        await Presets.RefreshAsync(Presets.LastSelectedPresetId);
    }

    public void ReportInitializationFailure()
    {
        StatusText = shellText.StartupFailed;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (!CanStartApply)
        {
            return;
        }

        await LoadCurrentSessionAsync(shellText.CurrentSetupRefreshed);
        await Presets.RefreshAsync(activeSession.BasedOnPreset?.Id);
    }

    [RelayCommand]
    private void CloseTopModal()
    {
        switch (workspace.TopModal)
        {
            case null:
                break;
            case ShellModal.DeleteConfirmation:
            case ShellModal.ManagePresets:
            case ShellModal.SaveAs:
                Presets.CloseTopPresetModal(workspace.TopModal.Value);
                break;
            case ShellModal.Settings:
                CloseSettings();
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(workspace.TopModal),
                    workspace.TopModal,
                    "Unknown shell modal layer.");
        }
    }

    private async Task LoadCurrentSessionAsync(string successStatus)
    {
        var result = await CurrentSessionLoader.LoadAsync(primaryMonitorDetector, fallbackMonitorDetector);
        workspace.ReplaceActiveSession(result.Session);

        RefreshSessionSurface(selectFirst: true);
        StatusText = shellText.CurrentSessionLoadResult(result.UsedFallback, successStatus, Monitors.Count);
    }

    private void RefreshSessionSurface(bool selectFirst)
    {
        RefreshRows(selectFirst);
        NotifySessionSummaryChanged();
    }

    private void RefreshRows(bool selectFirst)
    {
        var projection = MonitorRowsProjector.ReplaceRows(
            Monitors,
            MissingMonitors,
            activeSession,
            Text,
            Editor.SelectedMonitor?.MonitorKey,
            selectFirst);
        TopologyWidth = projection.TopologyWidth;
        TopologyHeight = projection.TopologyHeight;
        Editor.SelectProjectedMonitor(projection.SelectedMonitor);
        NotifyRowsSurfaceChanged();
    }

    private void NotifyModalStateChanged()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.ModalState);
        NotifyCommandStateChanged();
    }

    private bool TryOpenModal(ShellModal modal)
    {
        if (!workspace.TryOpenModal(modal))
        {
            return false;
        }

        NotifyModalLayerChanged(modal);
        NotifyModalStateChanged();
        return true;
    }

    private bool TryCloseModal(ShellModal modal)
    {
        if (workspace.TopModal != modal || !workspace.TryCloseTopModal(out _))
        {
            return false;
        }

        NotifyModalLayerChanged(modal);
        NotifyModalStateChanged();
        return true;
    }

    private void NotifyModalLayerChanged(ShellModal modal)
    {
        var properties = modal switch
        {
            ShellModal.Settings => ViewModelNotificationGroups.SettingsModalSurface,
            _ => throw new ArgumentOutOfRangeException(nameof(modal), modal, "Modal is not owned by MainPageViewModel."),
        };

        NotifyPropertiesChanged(properties);
    }

    private void NotifyRowsSurfaceChanged()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.RowsSurface);
    }

    private void NotifyCommandStateChanged()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.CommandState);
        Apply.NotifyWorkspaceStateChanged();
        Editor.NotifyWorkspaceStateChanged();
        Presets.NotifyWorkspaceStateChanged();
    }

    private void NotifySessionSummaryChanged() =>
        NotifyPropertiesChanged(ViewModelNotificationGroups.SessionSummarySurface);

    private void NotifyPropertiesChanged(params string[] propertyNames)
    {
        NotifyPropertiesChanged((IEnumerable<string>)propertyNames);
    }

    private void NotifyPropertiesChanged(IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in ViewModelNotificationGroups.Require(propertyNames))
        {
            OnPropertyChanged(propertyName);
        }
    }

}
