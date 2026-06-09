using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText(
    string CurrentSetup,
    string Preset,
    string PresetName,
    string Save,
    string SaveAs,
    string Manage,
    string Refresh,
    string Settings,
    string ApplyAll,
    string Apply,
    string Edit,
    string EditMonitor,
    string Source,
    string ImagePath,
    string ChooseImage,
    string Color,
    string Fit,
    string Anchor,
    string PositionX,
    string PositionY,
    string ResetPosition,
    string ManagePresets,
    string ManagePresetsSubtitle,
    string SaveAsPresetSubtitle,
    string NoPresetsSaved,
    string Close,
    string Rename,
    string Duplicate,
    string Delete,
    string DeleteSelectedPreset,
    string DeleteSelectedPresetFormat,
    string ConfirmDelete,
    string SettingsSubtitle,
    string Theme,
    string Language,
    string ThemeSystem,
    string ThemeLight,
    string ThemeDark,
    string LanguageEnglish,
    string LanguageSpanish,
    string ClearCache,
    string SaveSettings,
    string MissingSourcePrefix,
    string ModifiedSuffix,
    string DisconnectedSuffix,
    string VisualOnlySuffix,
    string NoMonitorSelected,
    string NoMonitorsDetected,
    string MissingMonitors,
    string MissingMonitorsSubtitle,
    string Forget,
    string Reassign,
    string MissingSource,
    string EmptySource,
    string ImageSource,
    string ColorSource,
    string Unsaved,
    string Saved,
    string Clean,
    string Pending,
    string Applying,
    string Applied,
    string Error,
    string CancelApply,
    string ApplyCancelled,
    string ImageSelectionCancelled,
    string SelectedImageFormat,
    string SkippedMissingSourceFormat,
    string SavedPresetFormat,
    string SavedNewPresetFormat,
    string SaveAsOpened,
    string ManagePresetsOpened,
    string SelectPresetToRename,
    string PresetNameRequired,
    string RenamedPresetFormat,
    string SelectPresetToDuplicate,
    string PresetNotFound,
    string PresetNotFoundFormat,
    string PresetLoadFailed,
    string DuplicatedPresetFormat,
    string SelectPresetToDelete,
    string DeletedPresetKeptSession,
    string SettingsOpened,
    string SettingsSaved,
    string LocalDataWriteFailed,
    string RenderedCacheClearedFormat,
    string RenderedCachePartiallyClearedFormat,
    string InvalidEditValueFormat,
    string InvalidColor,
    string ImagePathRequired,
    string ImagePathMustBeFull,
    string ImagePathUnsupportedFileType,
    string CheckValue,
    string RenderedWallpaperMissing,
    string WallpaperApplyFailed,
    string ForgotDisconnectedMonitorFormat,
    string ReassignedDisconnectedMonitorFormat,
    string SelectMonitorBeforeReassign,
    string PendingChangesFormat,
    string LoadedCurrentSetup,
    string CurrentSetupRefreshed,
    string MonitorCountFormat,
    string WindowsDetectionFallback,
    string StartupFailed,
    string CurrentSetupSelected,
    string LoadedPresetFormat,
    string PreparingApply,
    string NothingToApply,
    string NothingApplied,
    string ApplyUnexpectedFailure,
    string ApplyFinishedFormat)
{
    public string Format(string format, params object[] args) =>
        string.Format(
            AppLanguages.CultureFor(IsSpanish ? AppLanguages.Spanish : AppLanguages.English),
            format,
            args);

    public string ApplyResultSummary(ApplySessionResult result)
    {
        var summary = result.HasAppliedOutcome
            ? Format(ApplyFinishedFormat, result.Succeeded, result.Failed)
            : result.HasAnyOutcome
                ? NothingApplied
                : NothingToApply;

        return result.Skipped == 0
            ? summary
            : $"{summary} {Format(SkippedMissingSourceFormat, result.Skipped)}";
    }

    public string ApplyProgressSummary(ApplyProgress progress) =>
        progress.Total == 0
            ? NothingToApply
            : $"{ApplyStatus(progress.Status)} {progress.MonitorName} ({progress.Completed}/{progress.Total})";

    public string SelectedSourceWarning(WallpaperSource source) =>
        WallpaperSourceFiles.IsMissingImageFile(source)
            ? $"{MissingSourcePrefix}: {source.ImagePath}"
            : string.Empty;

    public string RenderedCacheClearSummary(RenderedCacheClearResult result) =>
        !result.HasFailures
            ? Format(RenderedCacheClearedFormat, result.Deleted)
            : Format(RenderedCachePartiallyClearedFormat, result.Deleted, result.Failed);

    public string ValidationMessage(ArgumentException error)
    {
        if (error is WallpaperSourcePathException pathError)
        {
            return pathError.ErrorCode switch
            {
                WallpaperSourcePathException.FullyQualifiedRequired => ImagePathMustBeFull,
                WallpaperSourcePathException.UnsupportedFileType => ImagePathUnsupportedFileType,
                _ => ImagePathRequired,
            };
        }

        return error.ParamName switch
        {
            "colorHex" => InvalidColor,
            "imagePath" => ImagePathRequired,
            _ => CheckValue,
        };
    }

    public string Resolution(int width, int height) => $"{width} x {height}";

    public string Bounds(int x, int y) => $"{x}, {y}";

    public string PlacementSummary(WallpaperPlacement placement) =>
        PlacementText.Summary(placement, IsSpanish);

    public string MonitorStatusSummary(
        MonitorApplyStatus applyStatus,
        string? applyError,
        bool isMissingImageSource,
        bool hasUnsavedPresetChanges)
    {
        var saved = hasUnsavedPresetChanges ? Unsaved : Saved;
        if (isMissingImageSource)
        {
            return $"{MissingSource} - {saved}";
        }

        if (applyStatus == MonitorApplyStatus.Error && !string.IsNullOrWhiteSpace(applyError))
        {
            return $"{Error}: {ApplyErrorLabel(applyError)} - {saved}";
        }

        return $"{ApplyStatus(applyStatus)} - {saved}";
    }

    public string SessionSummary(
        PresetIdentity? basedOnPreset,
        bool hasUnsavedPresetChanges,
        int missingAssignmentCount,
        PresetMenuItem? selectedPreset)
    {
        var name = basedOnPreset?.Name ?? CurrentSetup;
        var modified = hasUnsavedPresetChanges ? $" - {ModifiedSuffix}" : string.Empty;
        var missing = missingAssignmentCount > 0
            ? $" - {missingAssignmentCount} {DisconnectedSuffix}"
            : string.Empty;
        var visualOnly = basedOnPreset is null && selectedPreset?.Id is not null
            ? $" - {selectedPreset.Name} {VisualOnlySuffix}"
            : string.Empty;

        return $"{name}{modified}{missing}{visualOnly}";
    }

    public string ApplyStatus(Waller.Native.Core.Models.MonitorApplyStatus status) => status switch
    {
        Waller.Native.Core.Models.MonitorApplyStatus.Clean => Clean,
        Waller.Native.Core.Models.MonitorApplyStatus.Pending => Pending,
        Waller.Native.Core.Models.MonitorApplyStatus.Applying => Applying,
        Waller.Native.Core.Models.MonitorApplyStatus.Applied => Applied,
        Waller.Native.Core.Models.MonitorApplyStatus.Error => Error,
        _ => CheckValue,
    };

    public string ApplyErrorLabel(string applyError) => applyError switch
    {
        ApplyErrorCodes.MissingImageSource => MissingSource,
        ApplyErrorCodes.RenderedWallpaperMissing => RenderedWallpaperMissing,
        ApplyErrorCodes.WallpaperApplyFailed => WallpaperApplyFailed,
        _ => CheckValue,
    };

    public string SourceKind(WallpaperSourceKind source) => source switch
    {
        WallpaperSourceKind.Empty => EmptySource,
        WallpaperSourceKind.Image => ImageSource,
        WallpaperSourceKind.SolidColor => ColorSource,
        _ => CheckValue,
    };

    public string FitMode(WallpaperFitMode fit) =>
        PlacementText.FitMode(fit, IsSpanish);

    public string AnchorLabel(WallpaperAnchor anchor) =>
        PlacementText.AnchorLabel(anchor, IsSpanish);

    private bool IsSpanish => ReferenceEquals(this, Spanish);
}
