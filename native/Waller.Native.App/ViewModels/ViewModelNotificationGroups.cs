namespace Waller.Native.App.ViewModels;

internal static class ViewModelNotificationGroups
{
    public static IEnumerable<string> Require(IEnumerable<string> propertyNames)
    {
        ArgumentNullException.ThrowIfNull(propertyNames);

        foreach (var propertyName in propertyNames)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException(
                    "Property name collection cannot include blank items.",
                    nameof(propertyNames));
            }

            yield return propertyName;
        }
    }

    public static IEnumerable<string> CommandState =>
    [
        nameof(MainPageViewModel.CanStartApply),
        nameof(MainPageViewModel.CanEditSession),
        nameof(MainPageViewModel.CanUseShellCommands),
        nameof(MainPageViewModel.CanMutateManagedPresets),
        nameof(MainPageViewModel.CanUseModalActions),
    ];

    public static IEnumerable<string> EditPermission =>
    [
        nameof(MainPageViewModel.CanEditMonitorAssignment),
        nameof(MainPageViewModel.CanEditPlacement),
    ];

    public static IEnumerable<string> SelectedSourceWarning =>
    [
        nameof(MainPageViewModel.SelectedSourceWarning),
        nameof(MainPageViewModel.SelectedSourceWarningVisibility),
    ];

    public static IEnumerable<string> SessionSummarySurface =>
    [
        nameof(MainPageViewModel.SessionSummary),
    ];

    public static IEnumerable<string> DeleteConfirmationSurface =>
    [
        nameof(MainPageViewModel.DeleteConfirmationVisibility),
        nameof(MainPageViewModel.DeleteConfirmationMessage),
    ];

    public static IEnumerable<string> LanguageRefreshSurface =>
    [
        nameof(MainPageViewModel.Text),
        nameof(MainPageViewModel.SessionSummary),
        nameof(MainPageViewModel.MonitorCountHeader),
        nameof(MainPageViewModel.SelectedMonitorDisplayName),
        nameof(MainPageViewModel.SelectedMonitorResolution),
        nameof(MainPageViewModel.SelectedSourceWarning),
        nameof(MainPageViewModel.SelectedSourceWarningVisibility),
        nameof(MainPageViewModel.DeleteConfirmationMessage),
        nameof(MainPageViewModel.ManagePresetEmptyVisibility),
    ];

    public static IEnumerable<string> ApplySurface =>
    [
        nameof(MainPageViewModel.ApplyProgressVisibility),
    ];

    public static IEnumerable<string> ManagePresetsModalSurface =>
    [
        nameof(MainPageViewModel.ManagePresetsVisibility),
    ];

    public static IEnumerable<string> SaveAsModalSurface =>
    [
        nameof(MainPageViewModel.SaveAsVisibility),
    ];

    public static IEnumerable<string> SettingsModalSurface =>
    [
        nameof(MainPageViewModel.SettingsVisibility),
    ];

    public static IEnumerable<string> ManagePresetListSurface =>
    [
        nameof(MainPageViewModel.ManagePresetEmptyVisibility),
    ];

    public static IEnumerable<string> ModalState =>
    [
        nameof(MainPageViewModel.IsAnyModalOpen),
    ];

    public static IEnumerable<string> SourceEditorVisibility =>
    [
        nameof(MainPageViewModel.ImageSourceEditorVisibility),
        nameof(MainPageViewModel.ColorSourceEditorVisibility),
    ];

    public static IEnumerable<string> SelectedMonitorSurface =>
    [
        nameof(MainPageViewModel.EditPanelVisibility),
        nameof(MainPageViewModel.SelectedMonitorDisplayName),
        nameof(MainPageViewModel.SelectedMonitorResolution),
        nameof(MainPageViewModel.SelectedMonitorDisplayIndex),
    ];

    public static IEnumerable<string> RowsSurface =>
    [
        nameof(MainPageViewModel.NoMonitorsVisibility),
        nameof(MainPageViewModel.TopologyVisibility),
        nameof(MainPageViewModel.MissingMonitorsVisibility),
        nameof(MainPageViewModel.TopologyMonitors),
        nameof(MainPageViewModel.MonitorCountHeader),
    ];
}
