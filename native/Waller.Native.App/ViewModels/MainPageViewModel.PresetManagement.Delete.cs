using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
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
}
