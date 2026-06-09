using System.Collections.ObjectModel;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal static class ManagedPresetList
{
    public static async Task<PresetMenuItem?> RefreshAsync(
        PresetStore presetStore,
        ObservableCollection<PresetMenuItem> items,
        Guid? selectPresetId)
    {
        var presets = await presetStore.ListAsync();
        PresetMenuLists.ReplaceManage(items, presets);
        return PresetMenuLists.Select(items, selectPresetId);
    }
}
