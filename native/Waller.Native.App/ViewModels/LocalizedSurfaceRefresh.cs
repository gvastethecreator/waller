using System.Collections.ObjectModel;

namespace Waller.Native.App.ViewModels;

internal sealed record LocalizedSurfaceRefreshResult
{
    public LocalizedSurfaceRefreshResult(PresetMenuItem? SelectedPreset)
    {
        this.SelectedPreset = SelectedPreset;
    }

    public PresetMenuItem? SelectedPreset { get; }
}

internal static class LocalizedSurfaceRefresh
{
    public static LocalizedSurfaceRefreshResult Refresh(
        ObservableCollection<PresetMenuItem> presets,
        PresetMenuItem? selectedPreset,
        ObservableCollection<MonitorRowViewModel> monitors,
        ObservableCollection<MissingMonitorRowViewModel> missingMonitors,
        LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(missingMonitors);
        ArgumentNullException.ThrowIfNull(text);

        var refreshedSelectedPreset = PresetMenuLists.ReplaceCurrentSetupName(
            presets,
            selectedPreset,
            text.CurrentSetup);

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

        return new(refreshedSelectedPreset);
    }
}
