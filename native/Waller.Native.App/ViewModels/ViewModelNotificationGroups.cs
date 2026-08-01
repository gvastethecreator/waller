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
        nameof(MainPageViewModel.CanUseModalActions),
    ];

    public static IEnumerable<string> SessionSummarySurface =>
    [
        nameof(MainPageViewModel.SessionSummary),
    ];

    public static IEnumerable<string> LanguageRefreshSurface =>
    [
        nameof(MainPageViewModel.Text),
        nameof(MainPageViewModel.SessionSummary),
        nameof(MainPageViewModel.MonitorCountHeader),
    ];

    public static IEnumerable<string> SettingsModalSurface =>
    [
        nameof(MainPageViewModel.SettingsVisibility),
    ];

    public static IEnumerable<string> ModalState =>
    [
        nameof(MainPageViewModel.IsAnyModalOpen),
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
