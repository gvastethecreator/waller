using System.Collections.ObjectModel;

namespace Waller.Native.App.ViewModels;

internal static class LocalizedSurfaceRefresh
{
    public static void Refresh(
        ObservableCollection<MonitorRowViewModel> monitors,
        ObservableCollection<MissingMonitorRowViewModel> missingMonitors,
        LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(missingMonitors);
        ArgumentNullException.ThrowIfNull(text);

        foreach (var monitor in monitors)
        {
            (monitor ?? throw new ArgumentException(
                "Monitor collection cannot include null items.",
                nameof(monitors))).ReplaceText(text);
        }

        foreach (var monitor in missingMonitors)
        {
            (monitor ?? throw new ArgumentException(
                "Missing monitor collection cannot include null items.",
                nameof(missingMonitors))).ReplaceText(text);
        }

    }
}
