using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadCurrentSessionAsync(shellText.LoadedCurrentSetup);
        await RefreshPresetListAsync(selectPresetId: lastSelectedPresetId);
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
        await RefreshPresetListAsync(activeSession.BasedOnPreset?.Id);
    }

    [RelayCommand]
    private void CloseTopModal()
    {
        ShellModalClose.Dispatch(
            InteractionState.TopModal,
            ClearPendingDeletePreset,
            CloseManagePresets,
            CloseSaveAs,
            CloseSettings);
    }

    private async Task LoadCurrentSessionAsync(string successStatus)
    {
        var result = await CurrentSessionLoader.LoadAsync(primaryMonitorDetector, fallbackMonitorDetector);
        activeSession = result.Session;

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
            SelectedMonitor?.MonitorKey,
            selectFirst);
        TopologyWidth = projection.TopologyWidth;
        TopologyHeight = projection.TopologyHeight;
        SelectedMonitor = projection.SelectedMonitor;
        NotifyRowsSurfaceChanged();
    }

    private void NotifyModalStateChanged()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.ModalState);
        NotifyCommandStateChanged();
    }

    private void NotifySelectedMonitorSurfaceChanged()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SelectedMonitorSurface);
        NotifySelectedSourceWarningChanged();
    }

    private void NotifyRowsSurfaceChanged()
    {
        NotifySelectedSourceWarningChanged();
        NotifyPropertiesChanged(ViewModelNotificationGroups.RowsSurface);
    }

    private void NotifyCommandStateChanged()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.CommandState);
        NotifyEditPermissionChanged();
    }

    private void NotifyEditPermissionChanged() =>
        NotifyPropertiesChanged(ViewModelNotificationGroups.EditPermission);

    private void NotifySelectedSourceWarningChanged() =>
        NotifyPropertiesChanged(ViewModelNotificationGroups.SelectedSourceWarning);

    private void NotifySessionSummaryChanged() =>
        NotifyPropertiesChanged(ViewModelNotificationGroups.SessionSummarySurface);

    private void NotifyDeleteConfirmationSurfaceChanged() =>
        NotifyPropertiesChanged(ViewModelNotificationGroups.DeleteConfirmationSurface);

    private void NotifyPropertiesChanged(params string[] propertyNames)
    {
        NotifyPropertiesChanged((IEnumerable<string>)propertyNames);
    }

    private void NotifyPropertiesChanged(IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void ClearPendingDeletePreset()
    {
        pendingDeletePreset = null;
        IsDeleteConfirmationOpen = false;
        NotifyDeleteConfirmationSurfaceChanged();
    }
}
