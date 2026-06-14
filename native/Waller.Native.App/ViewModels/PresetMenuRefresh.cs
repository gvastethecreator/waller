using System.Collections.ObjectModel;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetMenuRefreshResult
{
    public PresetMenuRefreshResult(
        PresetMenuItem SelectedPreset,
        Guid? LastSelectedPresetId,
        bool RequestedPresetMissing)
    {
        ArgumentNullException.ThrowIfNull(SelectedPreset);
        var normalizedLastSelectedPresetId = PresetIds.NormalizeOptional(LastSelectedPresetId);

        if (RequestedPresetMissing && normalizedLastSelectedPresetId is not null)
        {
            throw new ArgumentException("Missing requested Preset refresh results cannot keep visual-memory id.", nameof(LastSelectedPresetId));
        }

        this.SelectedPreset = SelectedPreset;
        this.LastSelectedPresetId = normalizedLastSelectedPresetId;
        this.RequestedPresetMissing = RequestedPresetMissing;
    }

    public PresetMenuItem SelectedPreset { get; }

    public Guid? LastSelectedPresetId { get; }

    public bool RequestedPresetMissing { get; }
}

internal static class PresetMenuRefresh
{
    public static async Task<PresetMenuRefreshResult> RefreshMainAsync(
        PresetStore presetStore,
        ObservableCollection<PresetMenuItem> items,
        string currentSetupName,
        Guid? selectPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(items);
        var normalizedCurrentSetupName = PresetMenuDisplayName.Normalize(
            currentSetupName,
            nameof(currentSetupName));
        var normalizedSelectPresetId = PresetIds.NormalizeOptional(selectPresetId);

        var presets = await presetStore.ListAsync();
        PresetMenuLists.ReplaceMain(items, presets, normalizedCurrentSetupName);
        var selected = PresetMenuLists.Select(items, normalizedSelectPresetId);
        var requestedPresetMissing = normalizedSelectPresetId is not null && selected?.Id != normalizedSelectPresetId;
        return new PresetMenuRefreshResult(
            selected ?? throw new InvalidOperationException("Preset menu refresh did not produce a selected item."),
            requestedPresetMissing ? null : selected.Id,
            requestedPresetMissing);
    }
}
