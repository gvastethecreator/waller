using System.Collections.ObjectModel;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetMenuRefreshResult(
    PresetMenuItem? SelectedPreset,
    Guid? LastSelectedPresetId,
    bool RequestedPresetMissing);

internal static class PresetMenuRefresh
{
    public static async Task<PresetMenuRefreshResult> RefreshMainAsync(
        PresetStore presetStore,
        ObservableCollection<PresetMenuItem> items,
        string currentSetupName,
        Guid? selectPresetId)
    {
        var presets = await presetStore.ListAsync();
        PresetMenuLists.ReplaceMain(items, presets, currentSetupName);
        var selected = PresetMenuLists.Select(items, selectPresetId);
        return new PresetMenuRefreshResult(
            selected,
            selected?.Id,
            selectPresetId is not null && selected?.Id != selectPresetId);
    }
}
