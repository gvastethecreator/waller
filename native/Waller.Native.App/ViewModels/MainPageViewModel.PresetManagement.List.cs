namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
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
