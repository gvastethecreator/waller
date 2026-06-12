using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
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
}
