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
}
