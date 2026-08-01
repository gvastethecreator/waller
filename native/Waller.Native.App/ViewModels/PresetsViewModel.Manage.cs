using CommunityToolkit.Mvvm.Input;
using Waller.Native.Workflows.Presets;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class PresetsViewModel
{
    [RelayCommand]
    private async Task ManagePresets()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        await RefreshManagedPresetListAsync(activeSession.BasedOnPreset?.Id);
        if (TryOpenModal(ShellModal.ManagePresets))
        {
            SetStatus(presetText.ManageOpened);
        }
    }

    [RelayCommand]
    private void CloseManagePresets()
    {
        ClearPendingDeletePreset();
        TryCloseModal(ShellModal.ManagePresets);
    }

    [RelayCommand]
    private async Task RenameManagedPreset()
    {
        if (!CanMutateManagedPresets)
        {
            return;
        }

        if (!ManagedPresetCommandInput.TryRename(
            SelectedManagedPreset,
            ManagedPresetNameDraft,
            presetText,
            out var input,
            out var statusText))
        {
            SetStatus(statusText);
            return;
        }

        var result = await workflow.RenameAsync(input.Id, input.NameDraft);
        if (!result.TryGetValue(out var renamed))
        {
            await PresentFailureAsync(result.Status);
            return;
        }

        if (activeSession.BasedOnPreset?.Id == renamed.Id)
        {
            workspace.ReplaceActiveSession(activeSession with { BasedOnPreset = renamed.Identity });
            selectedPresetRecord = renamed;
            PresetNameDraft = renamed.Name;
            notifySessionSummary();
        }

        await RefreshAsync(activeSession.BasedOnPreset?.Id);
        await RefreshManagedPresetListAsync(renamed.Id);
        SetStatus(presetText.Renamed(renamed.Name));
    }

    [RelayCommand]
    private async Task DuplicateManagedPreset()
    {
        if (!CanMutateManagedPresets)
        {
            return;
        }

        if (!ManagedPresetCommandInput.TryDuplicate(
            SelectedManagedPreset,
            ManagedPresetNameDraft,
            presetText,
            out var input,
            out var statusText))
        {
            SetStatus(statusText);
            return;
        }

        var result = await workflow.DuplicateAsync(input.Id, input.NameDraft);
        if (!result.TryGetValue(out var duplicate))
        {
            await PresentFailureAsync(result.Status);
            return;
        }

        await RefreshAsync(activeSession.BasedOnPreset?.Id);
        await RefreshManagedPresetListAsync(duplicate.Id);
        SetStatus(presetText.Duplicated(duplicate.Name));
    }

    [RelayCommand]
    private void RequestDeleteManagedPreset()
    {
        if (!CanMutateManagedPresets)
        {
            return;
        }

        if (!ManagedPresetCommandInput.TryDeleteConfirmation(
            SelectedManagedPreset,
            presetText,
            out var confirmation,
            out var statusText))
        {
            SetStatus(statusText);
            return;
        }

        pendingDeletePreset = confirmation;
        OnPropertyChanged(nameof(DeleteConfirmationMessage));
        if (!TryOpenModal(ShellModal.DeleteConfirmation))
        {
            pendingDeletePreset = null;
            OnPropertyChanged(nameof(DeleteConfirmationMessage));
        }
    }

    [RelayCommand]
    private async Task ConfirmDeleteManagedPreset()
    {
        if (!CanUseModalActions || pendingDeletePreset is not { } target)
        {
            return;
        }

        var result = await workflow.DeleteAsync(activeSession, target.Id);
        if (!result.TryGetValue(out var deletion))
        {
            await PresentFailureAsync(result.Status);
            return;
        }

        if (deletion.DeletedActivePreset)
        {
            selectedPresetRecord = null;
            LastSelectedPresetId = null;
            PresetNameDraft = string.Empty;
            workspace.ReplaceActiveSession(deletion.Session);
            refreshSessionSurface(false);
        }

        ClearPendingDeletePreset();
        await RefreshAsync(deletion.DeletedActivePreset ? null : activeSession.BasedOnPreset?.Id);
        await PersistLastSelectedPresetAsync(SelectedPreset?.Id);
        await RefreshManagedPresetListAsync(selectPresetId: null);
        SetStatus(presetText.DeletedKeptSession);
    }
}
