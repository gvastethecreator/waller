namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    private void UpdateSelectedAssignment()
    {
        if (isRefreshingEditor || !CanEditMonitorAssignment || SelectedMonitor is null || Monitors.Count == 0)
        {
            return;
        }

        var result = MonitorAssignmentUpdate.ApplyFromEditorFields(
            sessionEditor,
            activeSession,
            SelectedMonitor.MonitorKey,
            EditSourceKind,
            EditImagePath,
            EditColorHex,
            EditColor,
            EditFitMode,
            EditAnchor,
            EditOffsetXPercent,
            EditOffsetYPercent);
        StatusText = result.StatusText(monitorEditText, SelectedMonitor.DisplayName);
        if (!result.TryGetUpdatedSession(out var updatedSession))
        {
            return;
        }

        activeSession = updatedSession;
        RefreshSessionSurface(selectFirst: false);
        NotifySelectedSourceWarningChanged();
    }
}
