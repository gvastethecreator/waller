namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    private async Task LoadSelectedPresetAsync(PresetMenuItem item, int loadVersion)
    {
        SelectedPresetLoadResult result;
        try
        {
            result = await localState.LoadSelectedPresetAsync(presetMatcher, activeSession, item);
        }
        catch (Exception)
        {
            if (loadVersion == selectedPresetLoadVersion)
            {
                StatusText = presetText.LoadFailed;
            }

            return;
        }

        if (loadVersion != selectedPresetLoadVersion)
        {
            return;
        }

        if (result.TryGetSelection(out var selection))
        {
            await ApplySelectedPresetSessionAsync(selection);
        }

        StatusText = result.StatusText(presetText);
        if (result.ShouldRefreshPresetList)
        {
            await RefreshPresetListAsync(selectPresetId: null);
        }
    }

    private async Task RefreshPresetListAsync(Guid? selectPresetId)
    {
        selectedPresetLoadVersion++;
        isChangingPresetSelection = true;
        try
        {
            var result = await localState.RefreshMainPresetsAsync(Presets, Text.CurrentSetup, selectPresetId);
            SelectedPreset = result.SelectedPreset;
            lastSelectedPresetId = result.LastSelectedPresetId;
            if (result.RequestedPresetMissing)
            {
                await PersistLastSelectedPresetAsync(null);
            }
        }
        finally
        {
            isChangingPresetSelection = false;
        }
    }

    private async Task PersistLastSelectedPresetAsync(Guid? presetId)
    {
        lastSelectedPresetId = await localState.PersistLastSelectedPresetAsync(presetId);
    }
}
