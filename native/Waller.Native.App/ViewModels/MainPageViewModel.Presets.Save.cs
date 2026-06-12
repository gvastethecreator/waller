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
}
