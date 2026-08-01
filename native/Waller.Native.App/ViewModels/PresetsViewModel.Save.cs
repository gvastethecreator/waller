using CommunityToolkit.Mvvm.Input;
using Waller.Native.Core.Models;
using Waller.Native.Workflows.Presets;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class PresetsViewModel
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

        var result = await workflow.SaveExistingAsync(activeSession, selectedPresetRecord);
        if (!result.TryGetValue(out var savedPreset))
        {
            await PresentFailureAsync(result.Status);
            return;
        }

        await CompleteSaveAsync(savedPreset, updateNameDraft: false);
        SetStatus(presetText.Saved(savedPreset.Name));
    }

    [RelayCommand]
    private void SaveAs()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        SaveAsPresetNameDraft = PresetNameInput.DraftForSaveAs(PresetNameDraft, DateTimeOffset.Now);
        if (TryOpenModal(ShellModal.SaveAs))
        {
            SetStatus(presetText.SaveAsOpened);
        }
    }

    [RelayCommand]
    private void CloseSaveAs()
    {
        TryCloseModal(ShellModal.SaveAs);
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
            SetStatus(statusText);
            return;
        }

        var result = await workflow.SaveAsAsync(activeSession, name);
        if (!result.TryGetValue(out var savedPreset))
        {
            await PresentFailureAsync(result.Status);
            return;
        }

        await CompleteSaveAsync(savedPreset, updateNameDraft: true);
        TryCloseModal(ShellModal.SaveAs);
        SetStatus(presetText.SavedNew(savedPreset.Name));
    }

    private async Task CompleteSaveAsync(Preset preset, bool updateNameDraft)
    {
        selectedPresetRecord = preset;
        if (updateNameDraft)
        {
            PresetNameDraft = preset.Name;
        }

        workspace.ReplaceActiveSession(activeSession.WithSavedPreset(preset.Identity));
        await RefreshAsync(preset.Id);
        await PersistLastSelectedPresetAsync(preset.Id);
        refreshSessionSurface(false);
    }
}
