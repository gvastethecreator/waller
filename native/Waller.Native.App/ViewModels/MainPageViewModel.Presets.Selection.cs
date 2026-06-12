namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    private async Task ApplySelectedPresetSessionAsync(SelectedPresetSession selection)
    {
        selectedPresetRecord = selection.SelectedPresetRecord;
        lastSelectedPresetId = selection.LastSelectedPresetId;
        activeSession = selection.Session;
        PresetNameDraft = selection.PresetNameDraft;
        RefreshSessionSurface(selection.SelectFirst);
        if (selection.PersistVisualMemory)
        {
            await PersistLastSelectedPresetAsync(selection.PersistPresetId);
        }
    }

    private void ApplyActivePresetRename(ActivePresetRename rename)
    {
        selectedPresetRecord = rename.SelectedPresetRecord;
        activeSession = rename.Session;
        PresetNameDraft = rename.PresetNameDraft;
        NotifySessionSummaryChanged();
    }
}
