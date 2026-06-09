using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private async Task ManagePresets()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        await RefreshManagePresetListAsync(activeSession.BasedOnPreset?.Id);
        IsManagePresetsOpen = true;
        StatusText = presetText.ManageOpened;
    }

    [RelayCommand]
    private void CloseManagePresets()
    {
        ClearPendingDeletePreset();
        IsManagePresetsOpen = false;
    }

    [RelayCommand]
    private async Task RenameManagedPreset()
    {
        if (!CanMutateManagedPresets)
        {
            return;
        }

        if (!ManagedPresetCommandInput.TryRename(
            SelectedManagePreset,
            ManagePresetNameDraft,
            presetText,
            out var input,
            out var statusText))
        {
            StatusText = statusText;
            return;
        }

        var result = await localState.RenameManagedPresetAsync(input.Id, input.NameDraft);
        if (await PresentManagedPresetFailureAsync(result.Missing, result.WriteFailed))
        {
            return;
        }

        if (!result.TryGetValue(out var renamed))
        {
            StatusText = shellText.LocalDataWriteFailed;
            return;
        }

        if (ActivePresetSession.IsBasedOn(activeSession, renamed.Id))
        {
            ApplyActivePresetRename(ActivePresetSession.RenameActive(activeSession, renamed));
        }

        await RefreshPresetListAsync(activeSession.BasedOnPreset?.Id);
        await RefreshManagePresetListAsync(renamed.Id);
        StatusText = presetText.Renamed(renamed.Name);
    }

    [RelayCommand]
    private async Task DuplicateManagedPreset()
    {
        if (!CanMutateManagedPresets)
        {
            return;
        }

        if (!ManagedPresetCommandInput.TryDuplicate(
            SelectedManagePreset,
            ManagePresetNameDraft,
            presetText,
            out var input,
            out var statusText))
        {
            StatusText = statusText;
            return;
        }

        var result = await localState.DuplicateManagedPresetAsync(input.Id, input.NameDraft);
        if (await PresentManagedPresetFailureAsync(result.Missing, result.WriteFailed))
        {
            return;
        }

        if (!result.TryGetValue(out var duplicate))
        {
            StatusText = shellText.LocalDataWriteFailed;
            return;
        }

        await RefreshPresetListAsync(activeSession.BasedOnPreset?.Id);
        await RefreshManagePresetListAsync(duplicate.Id);
        StatusText = presetText.Duplicated(duplicate.Name);
    }

    [RelayCommand]
    private void RequestDeleteManagedPreset()
    {
        if (!CanMutateManagedPresets)
        {
            return;
        }

        if (!ManagedPresetCommandInput.TryDeleteConfirmation(
            SelectedManagePreset,
            presetText,
            out var confirmation,
            out var statusText))
        {
            StatusText = statusText;
            return;
        }

        pendingDeletePreset = confirmation;
        NotifyDeleteConfirmationSurfaceChanged();
        IsDeleteConfirmationOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDeleteManagedPreset()
    {
        if (!CanUseModalActions)
        {
            return;
        }

        if (pendingDeletePreset is not { } target)
        {
            return;
        }

        var result = await localState.DeleteManagedPresetAsync(activeSession, target);
        if (await PresentManagedPresetFailureAsync(result.Missing, result.WriteFailed))
        {
            return;
        }

        if (!result.TryGetSuccessfulReplacement(out var replacementSelection))
        {
            StatusText = shellText.LocalDataWriteFailed;
            return;
        }

        if (replacementSelection is not null)
        {
            await ApplySelectedPresetSessionAsync(replacementSelection);
        }

        ClearPendingDeletePreset();
        await RefreshPresetListAsync(result.DeletedActivePreset ? null : activeSession.BasedOnPreset?.Id);
        await PersistLastSelectedPresetAsync(SelectedPreset?.Id);
        await RefreshManagePresetListAsync(selectPresetId: null);
        StatusText = presetText.DeletedKeptSession;
    }

    private async Task RefreshManagePresetListAsync(Guid? selectPresetId)
    {
        SelectedManagePreset = await localState.RefreshManagedPresetsAsync(ManagePresetItems, selectPresetId);
        NotifyPropertiesChanged(ViewModelNotificationGroups.ManagePresetListSurface);
    }

    private async Task HandleManagedPresetMissingAsync()
    {
        StatusText = presetText.MissingPreset;
        await RefreshPresetListAsync(activeSession.BasedOnPreset?.Id);
        await RefreshManagePresetListAsync(selectPresetId: null);
    }

    private async Task<bool> PresentManagedPresetFailureAsync(bool missing, bool writeFailed)
    {
        if (missing)
        {
            await HandleManagedPresetMissingAsync();
            return true;
        }

        if (writeFailed)
        {
            StatusText = shellText.LocalDataWriteFailed;
            return true;
        }

        return false;
    }
}
