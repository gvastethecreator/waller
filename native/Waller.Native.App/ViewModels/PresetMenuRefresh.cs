using System.Collections.ObjectModel;
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
        if (LastSelectedPresetId == Guid.Empty)
        {
            throw new ArgumentException("Last selected Preset id cannot be empty.", nameof(LastSelectedPresetId));
        }

        if (RequestedPresetMissing && LastSelectedPresetId is not null)
        {
            throw new ArgumentException("Missing requested Preset refresh results cannot keep visual-memory id.", nameof(LastSelectedPresetId));
        }

        this.SelectedPreset = SelectedPreset;
        this.LastSelectedPresetId = LastSelectedPresetId;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(currentSetupName);
        if (selectPresetId == Guid.Empty)
        {
            throw new ArgumentException("Preset menu refresh selection id cannot be empty.", nameof(selectPresetId));
        }

        var presets = await presetStore.ListAsync();
        PresetMenuLists.ReplaceMain(items, presets, currentSetupName);
        var selected = PresetMenuLists.Select(items, selectPresetId);
        var requestedPresetMissing = selectPresetId is not null && selected?.Id != selectPresetId;
        return new PresetMenuRefreshResult(
            selected ?? throw new InvalidOperationException("Preset menu refresh did not produce a selected item."),
            requestedPresetMissing ? null : selected.Id,
            requestedPresetMissing);
    }
}
