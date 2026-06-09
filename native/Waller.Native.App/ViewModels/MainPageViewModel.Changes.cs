using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    partial void OnSelectedPresetChanged(PresetMenuItem? value)
    {
        NotifySessionSummaryChanged();
        if (isChangingPresetSelection || value is null)
        {
            return;
        }

        var loadVersion = ++selectedPresetLoadVersion;
        _ = LoadSelectedPresetAsync(value, loadVersion);
    }

    partial void OnSelectedManagePresetChanged(PresetMenuItem? value)
    {
        ManagePresetNameDraft = ManagedPresetSelection.NameDraft(value);
        ClearPendingDeletePreset();
    }

    partial void OnIsManagePresetsOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.ManagePresetsModalSurface);
        NotifyModalStateChanged();
    }

    partial void OnIsSaveAsOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SaveAsModalSurface);
        NotifyModalStateChanged();
    }

    partial void OnIsDeleteConfirmationOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.DeleteConfirmationSurface);
        NotifyModalStateChanged();
    }

    partial void OnIsSettingsOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SettingsModalSurface);
        NotifyModalStateChanged();
    }

    partial void OnSelectedThemePreferenceChanged(AppThemePreference value)
    {
        OnPropertyChanged(nameof(RequestedTheme));
        SelectedThemeOption = OptionItems.Select(ThemeOptions, value);
    }

    partial void OnSelectedThemeOptionChanged(OptionItem<AppThemePreference>? value)
    {
        if (value is not null)
        {
            SelectedThemePreference = value.Value;
        }
    }

    partial void OnSelectedLanguageOptionChanged(OptionItem<string>? value)
    {
        if (value is not null)
        {
            SelectedLanguage = value.Value;
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        RefreshSettingOptions();
        RefreshEditorOptions();
        var refresh = LocalizedSurfaceRefresh.Refresh(
            Presets,
            SelectedPreset,
            Monitors,
            MissingMonitors,
            Text);
        SelectedPreset = refresh.SelectedPreset;
        NotifyPropertiesChanged(ViewModelNotificationGroups.LanguageRefreshSurface);
    }

    partial void OnIsApplyingChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.ApplySurface);
        NotifyCommandStateChanged();
    }

    partial void OnSelectedMonitorChanged(MonitorRowViewModel? value)
    {
        var assignment = MonitorRowSelection.ApplySelection(Monitors, value);
        NotifySelectedMonitorSurfaceChanged();
        if (assignment is null)
        {
            return;
        }

        RefreshEditorFromAssignment(assignment);
    }

    partial void OnSelectedSourceOptionChanged(OptionItem<WallpaperSourceKind>? value)
    {
        if (value is not null)
        {
            EditSourceKind = value.Value;
        }
    }

    partial void OnSelectedFitOptionChanged(OptionItem<WallpaperFitMode>? value)
    {
        if (value is not null)
        {
            EditFitMode = value.Value;
        }
    }

    partial void OnSelectedAnchorOptionChanged(OptionItem<WallpaperAnchor>? value)
    {
        if (value is not null)
        {
            EditAnchor = value.Value;
        }
    }

    partial void OnEditSourceKindChanged(WallpaperSourceKind value)
    {
        SelectedSourceOption = OptionItems.Select(SourceOptions, value);
        RefreshSourceEditorVisibility();
        NotifyEditPermissionChanged();
        UpdateSelectedAssignment();
    }

    partial void OnEditImagePathChanged(string value) => UpdateSelectedAssignment();

    partial void OnEditColorHexChanged(string value)
    {
        if (isRefreshingColor)
        {
            return;
        }

        if (ColorHex.TryToColor(value, out var color))
        {
            isRefreshingColor = true;
            try
            {
                EditColor = color;
            }
            finally
            {
                isRefreshingColor = false;
            }
        }

        UpdateSelectedAssignment();
    }

    partial void OnEditColorChanged(Color value)
    {
        if (isRefreshingColor)
        {
            return;
        }

        isRefreshingColor = true;
        try
        {
            EditColorHex = ColorHex.FromColor(value);
        }
        finally
        {
            isRefreshingColor = false;
        }

        UpdateSelectedAssignment();
    }

    partial void OnEditFitModeChanged(WallpaperFitMode value)
    {
        SelectedFitOption = OptionItems.Select(FitOptions, value);
        UpdateSelectedAssignment();
    }

    partial void OnEditAnchorChanged(WallpaperAnchor value)
    {
        SelectedAnchorOption = OptionItems.Select(AnchorOptions, value);
        UpdateSelectedAssignment();
    }

    partial void OnEditOffsetXPercentChanged(double value) => UpdateSelectedAssignment();

    partial void OnEditOffsetYPercentChanged(double value) => UpdateSelectedAssignment();
}
