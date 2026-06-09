using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private async Task Save()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        if (activeSession.BasedOnPreset is null || selectedPresetRecord is null)
        {
            SaveAs();
            return;
        }

        var result = await localState.SaveExistingPresetAsync(activeSession, selectedPresetRecord);
        if (!result.TryGetPreset(out var savedPreset))
        {
            StatusText = shellText.LocalDataWriteFailed;
            return;
        }

        await CompletePresetSaveAsync(PresetSaveCompletion.Existing(savedPreset));
        StatusText = presetText.Saved(savedPreset.Name);
    }

    [RelayCommand]
    private void SaveAs()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        SaveAsPresetNameDraft = PresetNameInput.DraftForSaveAs(PresetNameDraft, DateTimeOffset.Now);
        IsSaveAsOpen = true;
        StatusText = presetText.SaveAsOpened;
    }

    [RelayCommand]
    private void CloseSaveAs()
    {
        IsSaveAsOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmSaveAs()
    {
        if (!CanUseModalActions)
        {
            return;
        }

        if (!PresetNameInput.TryValidateRequired(
            SaveAsPresetNameDraft,
            presetText,
            out var name,
            out var statusText))
        {
            StatusText = statusText;
            return;
        }

        var result = await localState.SaveAsPresetAsync(activeSession, name);
        if (!result.TryGetPreset(out var savedPreset))
        {
            StatusText = shellText.LocalDataWriteFailed;
            return;
        }

        await CompletePresetSaveAsync(PresetSaveCompletion.New(savedPreset));
        IsSaveAsOpen = false;
        StatusText = presetText.SavedNew(savedPreset.Name);
    }

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

    private async Task CompletePresetSaveAsync(PresetSaveCompletion completion)
    {
        var preset = completion.SelectedPresetRecord;
        selectedPresetRecord = preset;
        if (completion.PresetNameDraft is not null)
        {
            PresetNameDraft = completion.PresetNameDraft;
        }

        activeSession = ActivePresetSession.MarkSaved(activeSession, preset);
        await RefreshPresetListAsync(preset.Id);
        await PersistLastSelectedPresetAsync(preset.Id);
        RefreshSessionSurface(selectFirst: false);
    }

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
