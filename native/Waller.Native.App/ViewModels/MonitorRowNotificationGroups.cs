namespace Waller.Native.App.ViewModels;

internal static class MonitorRowNotificationGroups
{
    public static IEnumerable<string> CurrentMonitorText =>
    [
        nameof(MonitorRowViewModel.Resolution),
        nameof(MonitorRowViewModel.Bounds),
        nameof(MonitorRowViewModel.SourceSummary),
        nameof(MonitorRowViewModel.SourcePreviewTextVisibility),
        nameof(MonitorRowViewModel.PlacementSummary),
        nameof(MonitorRowViewModel.StatusSummary),
        nameof(MonitorRowViewModel.TopologyAccessibleName),
        nameof(MonitorRowViewModel.EditAccessibleName),
        nameof(MonitorRowViewModel.ApplyAccessibleName),
    ];

    public static IEnumerable<string> CurrentMonitorSession =>
    [
        nameof(MonitorRowViewModel.MonitorKey),
        nameof(MonitorRowViewModel.DisplayName),
        nameof(MonitorRowViewModel.Resolution),
        nameof(MonitorRowViewModel.Bounds),
        nameof(MonitorRowViewModel.SourceSummary),
        nameof(MonitorRowViewModel.SourcePreviewBrush),
        nameof(MonitorRowViewModel.SourcePreviewImageBrush),
        nameof(MonitorRowViewModel.HasSourcePreviewImage),
        nameof(MonitorRowViewModel.SourcePreviewImageVisibility),
        nameof(MonitorRowViewModel.SourcePreviewTextVisibility),
        nameof(MonitorRowViewModel.PlacementSummary),
        nameof(MonitorRowViewModel.StatusSummary),
        nameof(MonitorRowViewModel.TopologyAccessibleName),
        nameof(MonitorRowViewModel.EditAccessibleName),
        nameof(MonitorRowViewModel.ApplyAccessibleName),
    ];

    public static IEnumerable<string> MissingMonitorText =>
    [
        nameof(MissingMonitorRowViewModel.Resolution),
        nameof(MissingMonitorRowViewModel.Bounds),
        nameof(MissingMonitorRowViewModel.SourceSummary),
        nameof(MissingMonitorRowViewModel.SourcePreviewTextVisibility),
        nameof(MissingMonitorRowViewModel.PlacementSummary),
        nameof(MissingMonitorRowViewModel.ReassignAccessibleName),
        nameof(MissingMonitorRowViewModel.ForgetAccessibleName),
    ];
}
