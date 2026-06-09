using System.Collections.ObjectModel;

namespace Waller.Native.App.ViewModels;

internal sealed record LocalizedSurfaceRefreshResult(PresetMenuItem? SelectedPreset);

internal static class LocalizedSurfaceRefresh
{
    public static LocalizedSurfaceRefreshResult Refresh(
        ObservableCollection<PresetMenuItem> presets,
        PresetMenuItem? selectedPreset,
        ObservableCollection<MonitorRowViewModel> monitors,
        ObservableCollection<MissingMonitorRowViewModel> missingMonitors,
        LocalizedText text)
    {
        var refreshedSelectedPreset = PresetMenuLists.ReplaceCurrentSetupName(
            presets,
            selectedPreset,
            text.CurrentSetup);

        foreach (var monitor in monitors)
        {
            monitor.ReplaceText(text);
        }

        foreach (var monitor in missingMonitors)
        {
            monitor.ReplaceText(text);
        }

        return new(refreshedSelectedPreset);
    }
}
