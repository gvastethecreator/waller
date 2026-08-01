using CommunityToolkit.Mvvm.Input;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private void OpenSettings()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        if (TryOpenModal(ShellModal.Settings))
        {
            StatusText = shellText.SettingsOpened;
        }
    }

    [RelayCommand]
    private void CloseSettings()
    {
        TryCloseModal(ShellModal.Settings);
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        if (!CanUseModalActions)
        {
            return;
        }

        var request = SettingsSaveRequest.FromSelection(
            SelectedThemePreference,
            SelectedLanguage,
            Presets.SelectedPreset);
        var result = await localState.SaveSettingsAsync(request);
        StatusText = result.StatusText(shellText);
        if (result.TryGetSavedLastSelectedPresetId(out var savedLastSelectedPresetId))
        {
            Presets.SetLastSelectedPresetId(savedLastSelectedPresetId);
        }
    }

    [RelayCommand]
    private void ClearRenderedCache()
    {
        if (!CanUseModalActions)
        {
            return;
        }

        var result = localState.ClearRenderedCache();
        StatusText = shellText.RenderedCacheClearSummary(result);
    }

    private async Task LoadSettingsAsync()
    {
        var draft = await localState.LoadSettingsDraftAsync();
        SelectedThemePreference = draft.Theme;
        SelectedLanguage = draft.Language;
        Presets.SetLastSelectedPresetId(draft.LastSelectedPresetId);
    }

    private void RefreshSettingOptions()
    {
        var selection = LocalizedOptionSelections.RefreshSettings(
            ThemeOptions,
            LanguageOptions,
            Text,
            SelectedThemePreference,
            SelectedLanguage);
        SelectedThemeOption = selection.Theme;
        SelectedLanguageOption = selection.Language;
    }
}
