param(
    [string]$AppPath = ".\Waller.Native.App"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedPath = if ([System.IO.Path]::IsPathRooted($AppPath)) {
    $AppPath
}
else {
    Join-Path $nativeRoot $AppPath
}

if (-not (Test-Path -LiteralPath $resolvedPath)) {
    throw "App path not found: $resolvedPath"
}

function Get-NativeRelativePath {
    param([string]$Path)

    $root = (Resolve-Path -LiteralPath $nativeRoot).Path.TrimEnd("\", "/")
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if ($resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($root.Length).TrimStart("\", "/")
    }

    return $resolved
}

function Test-TextContracts {
    param(
        [array]$Contracts,
        [string]$DefaultPositionalMessage = "positional contract"
    )

    $violations = @()
    foreach ($contract in $Contracts) {
        $contractPath = Join-Path $resolvedPath $contract.Path
        if (-not (Test-Path -LiteralPath $contractPath)) {
            $violations += "$($contract.Path): file missing"
            continue
        }

        $contractText = Get-Content -LiteralPath $contractPath -Raw
        if ($contract.PositionalPattern -and $contractText -match $contract.PositionalPattern) {
            $message = if ($contract.PositionalMessage) { $contract.PositionalMessage } else { $DefaultPositionalMessage }
            $violations += "$($contract.Path): $message"
        }

        foreach ($required in $contract.Required) {
            if (-not $contractText.Contains($required)) {
                $violations += "$($contract.Path): $required"
            }
        }
    }

    return $violations
}

$loadedAsyncHandlers = @()
$inlinePropertyChangedHandlers = @()
$hardCodedStatusText = @()
$rawEnumFallbacks = @()
$directLocalWriteRecovery = @()
$mainViewModelStateLeaks = @()
$localizedTextProjectionLeaks = @()
$localAppDataPathLeaks = @()
$mainEditorSourcePlacementLeaks = @()
$mainViewModelMonolithicChangesFiles = @()
$mainViewModelMonolithicStateFiles = @()
$mainViewModelMonolithicSurfaceFiles = @()
$mainPresetManagementResponsibilityLeaks = @()
$mainViewModelMonolithicPresetsFiles = @()
$mainViewModelMonolithicEditorFiles = @()
$placementTextCatalogLeaks = @()
$localizedTextMonolithicCatalogMembers = @()
$localizedTextUnnamedCatalogArgs = @()
$appDataRootWithoutValidation = @()
$appCompositionWithoutValidation = @()
$appCurrentSessionLoaderFiles = @()
$presetMenuItemWithoutNameValidation = @()
$sourceSelectionDtoWithoutValidation = @()
$optionDtoWithoutValidation = @()
$presetSessionDtoWithoutValidation = @()
$settingsDtoWithoutValidation = @()
$managedPresetCommandDtoWithoutValidation = @()
$workflowResultDtoWithoutValidation = @()
$editorDtoWithoutValidation = @()
$presetMenuDtoWithoutValidation = @()
$surfaceProjectionDtoWithoutValidation = @()
$appDefinedEnumHelperFiles = @()

$appDefinedEnumHelperPath = Join-Path $resolvedPath "ViewModels\DefinedEnumValue.cs"
if (Test-Path -LiteralPath $appDefinedEnumHelperPath) {
    $appDefinedEnumHelperFiles += Get-NativeRelativePath $appDefinedEnumHelperPath
}
$localWriteRecoveryAllowList = @(
    "Waller.Native.App\Platform\LocalDataWriteGuard.cs"
)
$localAppDataPathAllowList = @(
    "Waller.Native.App\Platform\WallerAppDataPaths.cs"
)

$monolithicChangesPath = Join-Path $resolvedPath "ViewModels\MainPageViewModel.Changes.cs"
if (Test-Path -LiteralPath $monolithicChangesPath) {
    $mainViewModelMonolithicChangesFiles += Get-NativeRelativePath $monolithicChangesPath
}

$monolithicStatePath = Join-Path $resolvedPath "ViewModels\MainPageViewModel.State.cs"
if (Test-Path -LiteralPath $monolithicStatePath) {
    $mainViewModelMonolithicStateFiles += Get-NativeRelativePath $monolithicStatePath
}

$monolithicSurfacePath = Join-Path $resolvedPath "ViewModels\MainPageViewModel.Surface.cs"
if (Test-Path -LiteralPath $monolithicSurfacePath) {
    $mainViewModelMonolithicSurfaceFiles += Get-NativeRelativePath $monolithicSurfacePath
}

$monolithicPresetsPath = Join-Path $resolvedPath "ViewModels\MainPageViewModel.Presets.cs"
if (Test-Path -LiteralPath $monolithicPresetsPath) {
    $mainViewModelMonolithicPresetsFiles += Get-NativeRelativePath $monolithicPresetsPath
}

$monolithicEditorPath = Join-Path $resolvedPath "ViewModels\MainPageViewModel.Editor.cs"
if (Test-Path -LiteralPath $monolithicEditorPath) {
    $mainViewModelMonolithicEditorFiles += Get-NativeRelativePath $monolithicEditorPath
}

$appCurrentSessionLoaderPath = Join-Path $resolvedPath "ViewModels\CurrentSessionLoader.cs"
if (Test-Path -LiteralPath $appCurrentSessionLoaderPath) {
    $appCurrentSessionLoaderFiles += Get-NativeRelativePath $appCurrentSessionLoaderPath
}

$presetMenuItemPath = Join-Path $resolvedPath "ViewModels\PresetMenuItem.cs"
if (-not (Test-Path -LiteralPath $presetMenuItemPath)) {
    $presetMenuItemWithoutNameValidation += "Waller.Native.App\ViewModels\PresetMenuItem.cs: file missing"
}
else {
    $presetMenuItemText = Get-Content -LiteralPath $presetMenuItemPath -Raw
    if ($presetMenuItemText -notmatch 'PresetMenuDisplayName\.Normalize\s*\(\s*Name,\s*nameof\s*\(\s*Name\s*\)\s*\)' -or
        $presetMenuItemText -notmatch 'PresetIds\.RequireValid\s*\(\s*presetId,\s*nameof\s*\(\s*value\s*\)\s*\)' -or
        $presetMenuItemText -notmatch 'PresetMenuDisplayName\.Normalize\s*\(\s*value,\s*nameof\s*\(\s*value\s*\)\s*\)') {
        $presetMenuItemWithoutNameValidation += Get-NativeRelativePath $presetMenuItemPath
    }
}

$imageSelectionDraftPath = Join-Path $resolvedPath "ViewModels\ImageSelectionDraft.cs"
if (-not (Test-Path -LiteralPath $imageSelectionDraftPath)) {
    $sourceSelectionDtoWithoutValidation += "Waller.Native.App\ViewModels\ImageSelectionDraft.cs: file missing"
}
else {
    $imageSelectionDraftText = Get-Content -LiteralPath $imageSelectionDraftPath -Raw
    if ($imageSelectionDraftText -notmatch 'WallpaperSourcePath\.NormalizeImagePath\s*\(\s*ImagePath\s*\)' -or
        $imageSelectionDraftText -notmatch 'ImageDisplayName\.Normalize\s*\(\s*DisplayFileName,\s*nameof\s*\(\s*DisplayFileName\s*\)\s*\)' -or
        $imageSelectionDraftText -notmatch 'ImageDisplayName\.Normalize\s*\(\s*value,\s*nameof\s*\(\s*value\s*\)\s*\)') {
        $sourceSelectionDtoWithoutValidation += Get-NativeRelativePath $imageSelectionDraftPath
    }
}

$imageDisplayNamePath = Join-Path $resolvedPath "ViewModels\ImageDisplayName.cs"
if (-not (Test-Path -LiteralPath $imageDisplayNamePath)) {
    $sourceSelectionDtoWithoutValidation += "Waller.Native.App\ViewModels\ImageDisplayName.cs: file missing"
}
else {
    $imageDisplayNameText = Get-Content -LiteralPath $imageDisplayNamePath -Raw
    if ($imageDisplayNameText -notmatch 'internal\s+static\s+class\s+ImageDisplayName' -or
        $imageDisplayNameText -notmatch 'displayName\.Trim\(\)' -or
        $imageDisplayNameText -notmatch 'Image display name is required\.') {
        $sourceSelectionDtoWithoutValidation += Get-NativeRelativePath $imageDisplayNamePath
    }
}

$monitorSourceSelectionPath = Join-Path $resolvedPath "ViewModels\MonitorSourceSelection.cs"
if (-not (Test-Path -LiteralPath $monitorSourceSelectionPath)) {
    $sourceSelectionDtoWithoutValidation += "Waller.Native.App\ViewModels\MonitorSourceSelection.cs: file missing"
}
else {
    $monitorSourceSelectionText = Get-Content -LiteralPath $monitorSourceSelectionPath -Raw
    if ($monitorSourceSelectionText -notmatch 'DefinedEnumValue\.Require\s*\(' -or
        $monitorSourceSelectionText -notmatch 'WallpaperSourcePath\.NormalizeImagePath\s*\(' -or
        $monitorSourceSelectionText -notmatch 'ColorHexValue\.Normalize\s*\(' -or
        $monitorSourceSelectionText -match 'public\s+WallpaperSourceKind\s+SourceKind\s*\{[^}]*init') {
        $sourceSelectionDtoWithoutValidation += Get-NativeRelativePath $monitorSourceSelectionPath
    }
}

$optionItemPath = Join-Path $resolvedPath "ViewModels\OptionItem.cs"
if (-not (Test-Path -LiteralPath $optionItemPath)) {
    $optionDtoWithoutValidation += "Waller.Native.App\ViewModels\OptionItem.cs: file missing"
}
else {
    $optionItemText = Get-Content -LiteralPath $optionItemPath -Raw
    if ($optionItemText -notmatch 'OptionDisplayName\.Normalize\s*\(\s*DisplayName,\s*nameof\s*\(\s*DisplayName\s*\)\s*\)' -or
        $optionItemText -notmatch 'ArgumentNullException\.ThrowIfNull\s*\(\s*target\s*\)' -or
        $optionItemText -notmatch 'ArgumentNullException\.ThrowIfNull\s*\(\s*options\s*\)' -or
        $optionItemText -notmatch 'Option collection cannot include null items' -or
        $optionItemText -match 'public\s+sealed\s+record\s+OptionItem<[^>]+>\s*\(') {
        $optionDtoWithoutValidation += Get-NativeRelativePath $optionItemPath
    }
}

$optionDisplayNamePath = Join-Path $resolvedPath "ViewModels\OptionDisplayName.cs"
if (-not (Test-Path -LiteralPath $optionDisplayNamePath)) {
    $optionDtoWithoutValidation += "Waller.Native.App\ViewModels\OptionDisplayName.cs: file missing"
}
else {
    $optionDisplayNameText = Get-Content -LiteralPath $optionDisplayNamePath -Raw
    if ($optionDisplayNameText -notmatch 'internal\s+static\s+class\s+OptionDisplayName' -or
        $optionDisplayNameText -notmatch 'displayName\.Trim\(\)' -or
        $optionDisplayNameText -notmatch 'Option display name is required\.') {
        $optionDtoWithoutValidation += Get-NativeRelativePath $optionDisplayNamePath
    }
}

$colorSwatchOptionPath = Join-Path $resolvedPath "ViewModels\ColorSwatchOption.cs"
if (-not (Test-Path -LiteralPath $colorSwatchOptionPath)) {
    $optionDtoWithoutValidation += "Waller.Native.App\ViewModels\ColorSwatchOption.cs: file missing"
}
else {
    $colorSwatchOptionText = Get-Content -LiteralPath $colorSwatchOptionPath -Raw
    if ($colorSwatchOptionText -notmatch 'ColorHexValue\.Normalize\s*\(\s*Hex\s*\)' -or
        $colorSwatchOptionText -notmatch 'ArgumentNullException\s*\(\s*nameof\s*\(\s*Brush\s*\)\s*\)' -or
        $colorSwatchOptionText -match 'public\s+sealed\s+record\s+ColorSwatchOption\s*\(') {
        $optionDtoWithoutValidation += Get-NativeRelativePath $colorSwatchOptionPath
    }
}

$colorSwatchCatalogPath = Join-Path $resolvedPath "ViewModels\ColorSwatchCatalog.cs"
if (-not (Test-Path -LiteralPath $colorSwatchCatalogPath)) {
    $optionDtoWithoutValidation += "Waller.Native.App\ViewModels\ColorSwatchCatalog.cs: file missing"
}
else {
    $colorSwatchCatalogText = Get-Content -LiteralPath $colorSwatchCatalogPath -Raw
    if ($colorSwatchCatalogText -notmatch 'public\s+static\s+IReadOnlyList<ColorSwatchOption>\s+Defaults\s*\(\s*\)' -or
        $colorSwatchCatalogText -notmatch 'DefaultHexValues\s*\r?\n\s*\.Select\s*\(\s*ColorSwatchOption\.FromHex\s*\)\s*\r?\n\s*\.ToArray\s*\(\s*\)') {
        $optionDtoWithoutValidation += Get-NativeRelativePath $colorSwatchCatalogPath
    }
}

$presetSessionContracts = @(
    @{
        Path = "ViewModels\ActivePresetSession.cs"
        Required = @("PresetIds.RequireValid(presetId, nameof(presetId))", "ArgumentNullException.ThrowIfNull(Session)", "ArgumentNullException.ThrowIfNull(SelectedPresetRecord)", "PresetNames.Validate(PresetNameDraft, nameof(PresetNameDraft))")
        PositionalPattern = 'internal\s+sealed\s+record\s+ActivePresetRename\s*\('
    },
    @{
        Path = "ViewModels\PresetSessionSave.cs"
        Required = @("Preset ?? throw new ArgumentNullException", "ArgumentNullException.ThrowIfNull(presetStore)", "ArgumentNullException.ThrowIfNull(session)", "PresetNames.Validate(name, nameof(name))", "PresetFactory.CreateFromSession(session, presetName)")
        PositionalPattern = 'internal\s+sealed\s+record\s+PresetSessionSaveResult\s*\('
    },
    @{
        Path = "ViewModels\PresetSaveCompletion.cs"
        Required = @("ArgumentNullException.ThrowIfNull(SelectedPresetRecord)", "PresetNames.Validate(PresetNameDraft, nameof(PresetNameDraft))")
        PositionalPattern = 'internal\s+sealed\s+record\s+PresetSaveCompletion\s*\('
    },
    @{
        Path = "ViewModels\SelectedPresetSession.cs"
        Required = @("ArgumentNullException.ThrowIfNull(Session)", "ArgumentNullException.ThrowIfNull(PresetNameDraft)", "PresetIds.NormalizeOptional(LastSelectedPresetId)", "PresetIds.NormalizeOptional(PersistPresetId)", "ArgumentNullException.ThrowIfNull(matcher)")
        PositionalPattern = 'internal\s+sealed\s+record\s+SelectedPresetSession\s*\('
    },
    @{
        Path = "ViewModels\SelectedPresetSessionLoader.cs"
        Required = @("DefinedEnumValue.Require(", "PresetMenuDisplayName.Normalize(DisplayName, nameof(DisplayName))", "Missing Preset load results cannot include a selection", "ArgumentNullException.ThrowIfNull(text)", "InvalidStatusTextKind(Kind)", "throw new ArgumentOutOfRangeException(", "Unknown selected Preset load kind.", "ArgumentNullException.ThrowIfNull(presetStore)", "ArgumentNullException.ThrowIfNull(presetMatcher)", "ArgumentNullException.ThrowIfNull(activeSession)", "ArgumentNullException.ThrowIfNull(item)")
        PositionalPattern = 'internal\s+sealed\s+record\s+SelectedPresetLoadResult\s*\('
    },
    @{
        Path = "ViewModels\ManagedPresetMutation.cs"
        Required = @("Managed Preset mutation result cannot be both missing and write-failed", "Value is null", "ArgumentNullException.ThrowIfNull(presetStore)", "PresetNames.Validate(name, nameof(name))", "presetStore.RenameAsync(presetId, presetName)", "ArgumentNullException.ThrowIfNull(mutation)", "ArgumentNullException.ThrowIfNull(success)", "LocalDataWriteGuard.TryAsync(", "catch (FileNotFoundException)", "ManagedPresetMutationResult<T>.LocalWriteFailed()")
        PositionalPattern = 'internal\s+sealed\s+record\s+ManagedPresetMutationResult<[^>]+>\s*\('
    },
    @{
        Path = "ViewModels\ManagedPresetDelete.cs"
        Required = @("Managed Preset delete result cannot be both missing and write-failed", "Failed Managed Preset delete results cannot include replacement selection", "deletedActivePreset && result.TryGetValue(out _)", "ArgumentNullException.ThrowIfNull(presetStore)", "ArgumentNullException.ThrowIfNull(activeSession)", "ArgumentNullException.ThrowIfNull(target)")
        PositionalPattern = 'internal\s+sealed\s+record\s+ManagedPresetDeleteResult\s*\('
    }
)

$presetSessionDtoWithoutValidation += Test-TextContracts $presetSessionContracts "positional record"

$settingsContracts = @(
    @{
        Path = "ViewModels\SettingsPreferenceDraft.cs"
        Required = @("DefinedEnumValue.Require(", "AppLanguages.Normalize(Language)", "Settings language must be supported", "PresetIds.NormalizeOptional(LastSelectedPresetId)", "ArgumentNullException(nameof(settings))")
        PositionalPattern = 'internal\s+sealed\s+record\s+SettingsPreferenceDraft\s*\('
    },
    @{
        Path = "ViewModels\SettingsPreferenceStore.cs"
        Required = @("PresetIds.NormalizeOptional(LastSelectedPresetId)", "Failed Settings save results cannot include last selected Preset", "ArgumentNullException.ThrowIfNull(settingsStore)", "ArgumentNullException.ThrowIfNull(request)", "ArgumentNullException.ThrowIfNull(shellText)", "PresetIds.NormalizeOptional(presetId)")
        PositionalPattern = 'internal\s+sealed\s+record\s+SettingsPreferenceSaveResult\s*\('
    },
    @{
        Path = "ViewModels\SettingsSaveRequest.cs"
        Required = @("draft ?? throw new ArgumentNullException", "SettingsPreferenceDraft.FromSelection")
        PositionalPattern = 'internal\s+sealed\s+class\s+SettingsSaveRequest\s*\('
    },
    @{
        Path = "ViewModels\MainPageLocalState.cs"
        Required = @("private readonly WallerLocalDataStores stores", "ArgumentNullException.ThrowIfNull(stores)", "this.stores = stores", "RenderedCacheCleanup.Clear(stores.RenderedWallpapers)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\RenderedCacheCleanup.cs"
        Required = @("ArgumentNullException.ThrowIfNull(store)", "return store.Clear();")
        PositionalPattern = $null
    }
)

$settingsDtoWithoutValidation += Test-TextContracts $settingsContracts

$managedPresetCommandContracts = @(
    @{
        Path = "ViewModels\PresetNameInput.cs"
        Required = @("PresetNames.Validate(draft)", "ArgumentNullException.ThrowIfNull(text)", "statusText = text.NameRequired")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ManagedPresetCommandInput.cs"
        Required = @("PresetIds.RequireValid(Id, nameof(Id))", "NameDraft ?? string.Empty", "ArgumentNullException.ThrowIfNull(text)", "[NotNullWhen(true)] out ManagedPresetCommandInput? input", "[NotNullWhen(true)] out PresetDeleteConfirmation? confirmation")
        PositionalPattern = 'internal\s+sealed\s+record\s+ManagedPresetCommandInput\s*\('
    },
    @{
        Path = "ViewModels\PresetDeleteConfirmation.cs"
        Required = @("PresetIds.RequireValid(Id, nameof(Id))", "PresetMenuDisplayName.Normalize(Name, nameof(Name))", "ArgumentNullException(nameof(text))")
        PositionalPattern = 'internal\s+sealed\s+record\s+PresetDeleteConfirmation\s*\('
    },
    @{
        Path = "ViewModels\ManagedPresetSelection.cs"
        Required = @("public static Guid? SelectedId(PresetMenuItem? item)", "PresetIds.IsValid(selectedId)", "ArgumentNullException(nameof(item))")
        PositionalPattern = $null
    }
)

$managedPresetCommandDtoWithoutValidation += Test-TextContracts $managedPresetCommandContracts

$workflowResultContracts = @(
    @{
        Path = "ViewModels\WorkflowStatusText.cs"
        Required = @("internal static class WorkflowStatusText", "public static string Require(string statusText, string parameterName)", "ArgumentException.ThrowIfNullOrWhiteSpace(statusText, parameterName)", "return statusText;")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ApplyRunRequest.cs"
        Required = @("ArgumentNullException.ThrowIfNull(applyService)", "ArgumentNullException.ThrowIfNull(session)", "ArgumentNullException.ThrowIfNull(monitor)", "MonitorKeys.Require(monitor.MonitorKey, ""monitor.MonitorKey"")", "ApplyMonitorReadySourceAsync(")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MainPageViewModel.Apply.cs"
        Required = @("ArgumentNullException.ThrowIfNull(apply)", "ArgumentNullException.ThrowIfNull(state)", "ApplyRunUiState.Success(result, applyText)", "ApplyRunUiState.FromException(error, applyText)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ApplyRunUiState.cs"
        Required = @("ArgumentNullException.ThrowIfNull(Session)", "ArgumentNullException.ThrowIfNull(ProgressText)", "WorkflowStatusText.Require(StatusText, nameof(StatusText))", "ArgumentNullException(nameof(result))", "ArgumentNullException(nameof(text))", "ArgumentNullException(nameof(error))")
        PositionalPattern = 'internal\s+sealed\s+record\s+ApplyRunUiState\s*\('
    },
    @{
        Path = "ViewModels\MonitorAssignmentUpdate.cs"
        Required = @("Successful monitor assignment updates cannot include validation failures", "Failed monitor assignment updates must include exactly one validation failure", "ArgumentNullException(nameof(session))", "ArgumentNullException(nameof(error))", "ArgumentNullException.ThrowIfNull(text)", "ArgumentNullException.ThrowIfNull(editor)", "MonitorKeys.Require(monitorKey, nameof(monitorKey))")
        PositionalPattern = 'internal\s+sealed\s+record\s+MonitorAssignmentUpdateResult\s*\('
    }
)

$workflowResultDtoWithoutValidation += Test-TextContracts $workflowResultContracts

$editorContracts = @(
    @{
        Path = "ViewModels\MonitorEditDraft.cs"
        Required = @("DefinedEnumValue.Require(", "EditorOffsetPercent.NormalizeX", "EditorOffsetPercent.NormalizeY", "EditorOffsetPercent.ToPlacementOffsetX", "EditorOffsetPercent.ToPlacementOffsetY", "global::Waller.Native.Core.Models.ColorHexValue.Normalize", "ArgumentNullException.ThrowIfNull(assignment)", "MonitorKeys.Require(monitorKey, nameof(monitorKey))", "WallpaperSourceKind.Empty => WallpaperSource.Empty", "InvalidSourceKind(SourceKind)")
        PositionalPattern = 'internal\s+sealed\s+record\s+MonitorEditDraft\s*\('
    },
    @{
        Path = "ViewModels\EditorOffsetPercent.cs"
        Required = @("internal static class EditorOffsetPercent", "NormalizeX", "NormalizeY", "ToPlacementOffsetX", "ToPlacementOffsetY", "double.IsFinite(offsetPercent)", "Math.Clamp(offsetPercent, -100d, 100d)", "MidpointRounding.AwayFromZero", "WallpaperPlacement.ClampOffset")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\DisconnectedMonitorEdit.cs"
        Required = @("WorkflowStatusText.Require(StatusText, nameof(StatusText))", "ArgumentNullException(nameof(editor))", "ArgumentNullException(nameof(session))", "ArgumentNullException(nameof(monitor))", "ArgumentNullException(nameof(text))")
        PositionalPattern = 'internal\s+sealed\s+record\s+DisconnectedMonitorEditResult\s*\('
    },
    @{
        Path = "ViewModels\MonitorSourceSelection.cs"
        Required = @("WorkflowStatusText.Require(StatusText, nameof(StatusText))", "ArgumentNullException.ThrowIfNull(text)", "ArgumentNullException(nameof(swatch))")
        PositionalPattern = 'internal\s+sealed\s+record\s+ImageSourceSelectionResult\s*\('
    }
)

$editorDtoWithoutValidation += Test-TextContracts $editorContracts

$presetMenuContracts = @(
    @{
        Path = "ViewModels\PresetMenuDisplayName.cs"
        Required = @("internal static class PresetMenuDisplayName", "public static string Normalize(string name, string parameterName)", "throw new ArgumentNullException(parameterName)", 'throw new ArgumentException("Preset menu name is required.", parameterName)', "return trimmed;")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\PresetMenuLists.cs"
        Required = @("ArgumentNullException.ThrowIfNull(items)", "ArgumentNullException.ThrowIfNull(presets)", "PresetMenuDisplayName.Normalize", "PresetIds.RequireValid(presetId, nameof(id))", "Preset menu collection cannot include null items.", "private static PresetMenuItem? FirstOrDefault", "private static PresetMenuItem Item")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\PresetMenuRefresh.cs"
        Required = @("ArgumentNullException.ThrowIfNull(SelectedPreset)", "PresetIds.NormalizeOptional(LastSelectedPresetId)", "Missing requested Preset refresh results cannot keep visual-memory id", "ArgumentNullException.ThrowIfNull(presetStore)", "PresetMenuDisplayName.Normalize", "PresetIds.NormalizeOptional(selectPresetId)", "Preset menu refresh did not produce a selected item")
        PositionalPattern = 'internal\s+sealed\s+record\s+PresetMenuRefreshResult\s*\('
    },
    @{
        Path = "ViewModels\ManagedPresetList.cs"
        Required = @("ArgumentNullException.ThrowIfNull(presetStore)", "ArgumentNullException.ThrowIfNull(items)", "PresetMenuLists.ReplaceManage(items, presets)", "PresetMenuLists.Select(items, selectPresetId)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\LocalizedSurfaceRefresh.cs"
        Required = @("internal sealed record LocalizedSurfaceRefreshResult", "public PresetMenuItem? SelectedPreset", "ArgumentNullException.ThrowIfNull(presets)", "ArgumentNullException.ThrowIfNull(monitors)", "ArgumentNullException.ThrowIfNull(missingMonitors)", "ArgumentNullException.ThrowIfNull(text)", "Monitor collection cannot include null items.", "Missing monitor collection cannot include null items.")
        PositionalPattern = 'internal\s+sealed\s+record\s+LocalizedSurfaceRefreshResult\s*\('
    }
)

$presetMenuDtoWithoutValidation += Test-TextContracts $presetMenuContracts

$surfaceProjectionContracts = @(
    @{
        Path = "ViewModels\LocalizedText.cs"
        Required = @("public string Format(string format, params object[] args)", "ArgumentNullException.ThrowIfNull(format)", "ArgumentNullException.ThrowIfNull(args)", "AppLanguages.CultureFor")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\LocalizedTextSource.cs"
        Required = @("internal static class LocalizedTextSource", "public static Func<LocalizedText> Require(Func<LocalizedText> text)", "ArgumentNullException.ThrowIfNull(text)", "Localized text source returned null.")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MainPageTextPresenters.cs"
        Required = @("var source = LocalizedTextSource.Require(text)", "Apply = new ApplyTextPresenter(source)", "Preset = new PresetTextPresenter(source)", "MonitorEdit = new MonitorEditTextPresenter(source)", "Shell = new ShellStatusTextPresenter(source)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ViewModelNotificationGroups.cs"
        Required = @("public static IEnumerable<string> Require(IEnumerable<string> propertyNames)", "ArgumentNullException.ThrowIfNull(propertyNames)", "Property name collection cannot include blank items.")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ShellInteractionState.cs"
        Required = @("public ShellModalLayer TopModal", "IsDeleteConfirmationOpen ? ShellModalLayer.DeleteConfirmation", "public bool CanUseShellCommands => !IsApplying && !IsAnyModalOpen", "public bool CanUseModalActions => !IsApplying")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ShellModalClose.cs"
        Required = @("case ShellModalLayer.None:", "Invoke(closeDeleteConfirmation, nameof(closeDeleteConfirmation))", "Invoke(closeManagePresets, nameof(closeManagePresets))", "Invoke(closeSaveAs, nameof(closeSaveAs))", "Invoke(closeSettings, nameof(closeSettings))", "Unknown shell modal layer.")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ApplyTextPresenter.cs"
        Required = @("private readonly Func<LocalizedText> text", "this.text = LocalizedTextSource.Require(text)", "ArgumentNullException.ThrowIfNull(progress)", "ArgumentNullException.ThrowIfNull(result)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\PresetTextPresenter.cs"
        Required = @("private readonly Func<LocalizedText> text", "this.text = LocalizedTextSource.Require(text)", "PresetNames.Validate(name, nameof(name))")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MonitorEditTextPresenter.cs"
        Required = @("private readonly Func<LocalizedText> text", "this.text = LocalizedTextSource.Require(text)", "ImageDisplayName.Normalize(fileName, nameof(fileName))", "NormalizeMonitorName(monitorName, nameof(monitorName))", "NormalizeMonitorName(targetName, nameof(targetName))", "ArgumentNullException.ThrowIfNull(error)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ShellStatusTextPresenter.cs"
        Required = @("private readonly Func<LocalizedText> text", "this.text = LocalizedTextSource.Require(text)", "WorkflowStatusText.Require(successStatus, nameof(successStatus))")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\ThemePreferenceMapper.cs"
        Required = @("DefinedEnumValue.Require(", "Unknown theme preference.", "AppThemePreference.System => ElementTheme.Default", "InvalidThemePreference(preference)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\LocalizedOptionCatalog.cs"
        Required = @("public static IReadOnlyList<OptionItem<AppThemePreference>> ThemeOptions", "private static LocalizedText RequireText(LocalizedText text)", "ArgumentNullException.ThrowIfNull(text)", "var localizedText = RequireText(text)", "localizedText.ThemeSystem", "localizedText.SourceKind(source)", ".ToArray()")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\LocalizedOptionSelections.cs"
        Required = @("internal sealed record SettingsOptionSelection", "internal sealed record EditorOptionSelection", "ArgumentNullException.ThrowIfNull(themeOptions)", "ArgumentNullException.ThrowIfNull(text)", "DefinedEnumValue.Require(", "ValidateEditorSelection", "public OptionItem<AppThemePreference>? Theme", "public OptionItem<WallpaperSourceKind>? Source")
        PositionalPattern = 'internal\s+sealed\s+record\s+(SettingsOptionSelection|EditorOptionSelection)\s*\('
    },
    @{
        Path = "ViewModels\MonitorRowSelection.cs"
        Required = @("ArgumentNullException.ThrowIfNull(monitors)", "Monitor row selection cannot include null items.", "ReferenceEquals(monitor, selectedMonitor)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MonitorRowsProjector.cs"
        Required = @("ArgumentNullException.ThrowIfNull(monitors)", "ArgumentNullException.ThrowIfNull(missingMonitors)", "ArgumentNullException.ThrowIfNull(session)", "ArgumentNullException.ThrowIfNull(text)", "MonitorKeys.Equals(monitor.MonitorKey, selectedMonitorKey)", "Topology width must be positive and finite", "Topology height must be positive and finite")
        PositionalPattern = 'internal\s+sealed\s+record\s+MonitorRowsProjection\s*\('
    },
    @{
        Path = "ViewModels\MonitorRowViewModel.cs"
        Required = @("text ?? throw new ArgumentNullException(nameof(text))", "session ?? throw new ArgumentNullException(nameof(session))", "ArgumentNullException.ThrowIfNull(text)", "ArgumentNullException.ThrowIfNull(session)", "MonitorRowNotificationGroups.CurrentMonitorText", "MonitorRowNotificationGroups.CurrentMonitorSession", "ViewModelNotificationGroups.Require(propertyNames)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MissingMonitorRowViewModel.cs"
        Required = @("text ?? throw new ArgumentNullException(nameof(text))", "assignment ?? throw new ArgumentNullException(nameof(assignment))", "ArgumentNullException.ThrowIfNull(text)", "MonitorRowNotificationGroups.MissingMonitorText", "ViewModelNotificationGroups.Require(propertyNames)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MonitorRowsSurface.cs"
        Required = @("ArgumentNullException.ThrowIfNull(monitors)", "ArgumentNullException.ThrowIfNull(missingMonitors)", "VisibilityStates.When(monitors.Count == 0)", "VisibilityStates.Unless(monitors.Count == 0)", "VisibilityStates.Unless(missingMonitors.Count == 0)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\LocalizedText.Editor.cs"
        Required = @("DefinedEnumValue.Require(", "Unknown localized source kind.", "WallpaperSourceKind.Empty => EmptySource", "InvalidSourceKind(source)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MonitorEditorSurface.cs"
        Required = @("ArgumentNullException.ThrowIfNull(text)", "DefinedEnumValue.Require(", "Unknown editor source kind.", "VisibilityStates.When(sourceKind == WallpaperSourceKind.Image)", "VisibilityStates.When(sourceKind == WallpaperSourceKind.SolidColor)", "text.SelectedSourceWarning(source)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MonitorSourcePreview.cs"
        Required = @("ArgumentNullException.ThrowIfNull(source)", "ArgumentNullException.ThrowIfNull(placement)", "DefinedEnumValue.Require(", "Unknown preview source kind.", "InvalidSourceKind(source.Kind)", "PlacementPreview.StretchFor(placement)", "PlacementPreview.AlignmentXFor(placement)", "PlacementPreview.AlignmentYFor(placement)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\MonitorSourceText.cs"
        Required = @("ArgumentNullException.ThrowIfNull(source)", "ArgumentNullException.ThrowIfNull(text)", "DefinedEnumValue.Require(", "Unknown monitor source kind.", "WallpaperSourceKind.Empty => text.EmptySource", "InvalidSourceKind(source.Kind)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\PlacementPreview.cs"
        Required = @("ArgumentNullException.ThrowIfNull(placement)", "DefinedEnumValue.Require(", "Unknown preview fit mode.", "Unknown preview anchor.", "WallpaperFitMode.Cover => Stretch.UniformToFill", "InvalidFitMode(placement.FitMode)", "InvalidAlignmentXAnchor(placement.Anchor)", "InvalidAlignmentYAnchor(placement.Anchor)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\PlacementText.cs"
        Required = @("ArgumentNullException.ThrowIfNull(placement)", "ArgumentNullException.ThrowIfNull(text)", "DefinedEnumValue.Require(", "PlacementTextErrors.UnknownFitMode", "PlacementTextErrors.UnknownAnchor", "WallpaperFitMode.Cover => text.FitCover", "WallpaperAnchor.BottomRight => text.AnchorBottomRight", "InvalidFitMode(fit)", "InvalidAnchor(anchor)")
        PositionalPattern = $null
    },
    @{
        Path = "ViewModels\PlacementTextErrors.cs"
        Required = @("internal static class PlacementTextErrors", "public const string UnknownFitMode = ""Unknown placement fit mode.""", "public const string UnknownAnchor = ""Unknown placement anchor.""")
        PositionalPattern = $null
    }
)

$surfaceProjectionDtoWithoutValidation += Test-TextContracts $surfaceProjectionContracts

$appDataPathsPath = Join-Path $resolvedPath "Platform\WallerAppDataPaths.cs"
if (Test-Path -LiteralPath $appDataPathsPath) {
    $appDataPathsText = Get-Content -LiteralPath $appDataPathsPath -Raw
    if ($appDataPathsText -notmatch 'RootFor\s*\(\s*string\s+localApplicationDataPath\s*\)' -or
        $appDataPathsText -notmatch 'ArgumentException\.ThrowIfNullOrWhiteSpace\s*\(\s*localApplicationDataPath\s*\)') {
        $appDataRootWithoutValidation += Get-NativeRelativePath $appDataPathsPath
    }
}

$appCompositionContracts = @(
    @{
        Path = "Platform\WallerAppServices.cs"
        Parameters = @("PrimaryMonitorDetector", "FallbackMonitorDetector", "ImageFilePicker", "ApplyService", "LocalData")
    },
    @{
        Path = "Platform\WallerLocalDataStores.cs"
        Parameters = @("Presets", "Settings", "RenderedWallpapers")
    },
    @{
        Path = "ViewModels\MainPageViewModel.cs"
        Parameters = @("primaryMonitorDetector", "fallbackMonitorDetector", "imageFilePicker", "applyService", "localData")
    }
)

foreach ($contract in $appCompositionContracts) {
    $contractPath = Join-Path $resolvedPath $contract.Path
    if (-not (Test-Path -LiteralPath $contractPath)) {
        $appCompositionWithoutValidation += "$($contract.Path): file missing"
        continue
    }

    $contractText = Get-Content -LiteralPath $contractPath -Raw
    foreach ($parameter in $contract.Parameters) {
        if ($contractText -notmatch "ArgumentNullException\.ThrowIfNull\s*\(\s*$parameter\s*\)") {
            $appCompositionWithoutValidation += "$($contract.Path): $parameter"
        }
    }
}

foreach ($file in Get-ChildItem -LiteralPath $resolvedPath -Recurse -Filter *.cs) {
    if ($file.FullName -match "\\(bin|obj)\\") {
        continue
    }

    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        $relativePath = Get-NativeRelativePath $file.FullName
        if ($line -match 'Loaded\s*\+=\s*async\b') {
            $loadedAsyncHandlers += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($relativePath -eq "Waller.Native.App\MainPage.xaml.cs" -and
            $line -match 'PropertyChanged\s*\+=\s*\([^)]*\)\s*=>') {
            $inlinePropertyChangedHandlers += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '\b(StatusText|ApplyProgressText)\s*=\s*"') {
            $hardCodedStatusText += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '_\s*=>\s*.*\.ToString\(\)') {
            $rawEnumFallbacks += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match 'LocalDataWriteGuard\.IsRecoverable\s*\(') {
            if ($localWriteRecoveryAllowList -notcontains $relativePath) {
                $directLocalWriteRecovery += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\MainPageViewModel.cs") {
            if ($line -match '\[ObservableProperty\]|ObservableCollection<|public\s+Visibility\s+') {
                $mainViewModelStateLeaks += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\LocalizedText.cs") {
            if ($line -match 'public\s+string\s+(ApplyResultSummary|ApplyProgressSummary|SelectedSourceWarning|RenderedCacheClearSummary|ValidationMessage|Resolution|Bounds|PlacementSummary|MonitorStatusSummary|SessionSummary|SourceKind|FitMode|AnchorLabel|ApplyStatus|ApplyErrorLabel)\b') {
                $localizedTextProjectionLeaks += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\LocalizedText.Catalog.cs") {
            if ($line -match 'public\s+static\s+LocalizedText\s+(English|Spanish)\b') {
                $localizedTextMonolithicCatalogMembers += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -in @(
            "Waller.Native.App\ViewModels\LocalizedText.Catalog.English.cs",
            "Waller.Native.App\ViewModels\LocalizedText.Catalog.Spanish.cs")) {
            if ($line -match '^\s*"[^"]*"') {
                $localizedTextUnnamedCatalogArgs += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\PlacementText.cs") {
            if ($line -match '(?<!\$)"[^"]*[A-Za-z][^"]*"') {
                $placementTextCatalogLeaks += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\MainPageViewModel.Editor.cs") {
            if ($line -match 'private\s+(?:async\s+)?(?:Task|void)\s+(ChooseImage|SelectColorSwatch|ApplySourceSelection|RefreshSourceEditorVisibility|ResetPosition|SetEditOffsets)\b') {
                $mainEditorSourcePlacementLeaks += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\MainPageViewModel.PresetManagement.cs") {
            if ($line -match 'private\s+(?:async\s+)?(?:Task|void)\s+(RenameManagedPreset|DuplicateManagedPreset|RequestDeleteManagedPreset|ConfirmDeleteManagedPreset|RefreshManagePresetListAsync|HandleManagedPresetMissingAsync|PresentManagedPresetFailureAsync)\b') {
                $mainPresetManagementResponsibilityLeaks += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        if ($line -match 'SpecialFolder\.LocalApplicationData|LOCALAPPDATA|%LOCALAPPDATA%' -and
            $localAppDataPathAllowList -notcontains $relativePath) {
            $localAppDataPathLeaks += "${relativePath}:$lineNumber`: $($line.Trim())"
        }
    }
}

if ($loadedAsyncHandlers.Count -gt 0) {
    Write-Host "Inline async Loaded handlers found; use a named async void handler with try/catch:" -ForegroundColor Red
    foreach ($handler in $loadedAsyncHandlers) {
        Write-Host " - $handler" -ForegroundColor Red
    }

    exit 1
}

if ($inlinePropertyChangedHandlers.Count -gt 0) {
    Write-Host "Inline MainPage PropertyChanged handlers found; use named handlers so modal focus routing stays reviewable:" -ForegroundColor Red
    foreach ($handler in $inlinePropertyChangedHandlers) {
        Write-Host " - $handler" -ForegroundColor Red
    }

    exit 1
}

if ($hardCodedStatusText.Count -gt 0) {
    Write-Host "Hard-coded status/progress strings found; use LocalizedText presenters instead:" -ForegroundColor Red
    foreach ($statusText in $hardCodedStatusText) {
        Write-Host " - $statusText" -ForegroundColor Red
    }

    exit 1
}

if ($rawEnumFallbacks.Count -gt 0) {
    Write-Host "Raw enum fallback text found; use localized fallback copy instead:" -ForegroundColor Red
    foreach ($fallback in $rawEnumFallbacks) {
        Write-Host " - $fallback" -ForegroundColor Red
    }

    exit 1
}

if ($directLocalWriteRecovery.Count -gt 0) {
    Write-Host "Direct local write recovery catches found; use LocalDataWriteGuard.TryAsync or an approved local-state helper:" -ForegroundColor Red
    foreach ($recovery in $directLocalWriteRecovery) {
        Write-Host " - $recovery" -ForegroundColor Red
    }

    exit 1
}

if ($mainViewModelStateLeaks.Count -gt 0) {
    Write-Host "MainPageViewModel.cs state/surface leaks found; use focused MainPageViewModel.State.*.cs or MainPageViewModel.Surface.*.cs files:" -ForegroundColor Red
    foreach ($leak in $mainViewModelStateLeaks) {
        Write-Host " - $leak" -ForegroundColor Red
    }

    exit 1
}

if ($localizedTextProjectionLeaks.Count -gt 0) {
    Write-Host "LocalizedText.cs projection leaks found; use focused LocalizedText partial files:" -ForegroundColor Red
    foreach ($leak in $localizedTextProjectionLeaks) {
        Write-Host " - $leak" -ForegroundColor Red
    }

    exit 1
}

if ($localAppDataPathLeaks.Count -gt 0) {
    Write-Host "Direct local app-data path usage found; use WallerAppDataPaths/WallerLocalDataStores instead:" -ForegroundColor Red
    foreach ($leak in $localAppDataPathLeaks) {
        Write-Host " - $leak" -ForegroundColor Red
    }

    exit 1
}

if ($mainEditorSourcePlacementLeaks.Count -gt 0) {
    Write-Host "Main editor source/placement leaks found; use MainPageViewModel.Editor.Source.cs or MainPageViewModel.Editor.Placement.cs:" -ForegroundColor Red
    foreach ($leak in $mainEditorSourcePlacementLeaks) {
        Write-Host " - $leak" -ForegroundColor Red
    }

    exit 1
}

if ($mainViewModelMonolithicChangesFiles.Count -gt 0) {
    Write-Host "Monolithic MainPageViewModel change-hook files found; use focused MainPageViewModel.Changes.*.cs files:" -ForegroundColor Red
    foreach ($file in $mainViewModelMonolithicChangesFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($mainViewModelMonolithicStateFiles.Count -gt 0) {
    Write-Host "Monolithic MainPageViewModel state files found; use focused MainPageViewModel.State.*.cs files:" -ForegroundColor Red
    foreach ($file in $mainViewModelMonolithicStateFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($mainViewModelMonolithicSurfaceFiles.Count -gt 0) {
    Write-Host "Monolithic MainPageViewModel surface files found; use focused MainPageViewModel.Surface.*.cs files:" -ForegroundColor Red
    foreach ($file in $mainViewModelMonolithicSurfaceFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($mainPresetManagementResponsibilityLeaks.Count -gt 0) {
    Write-Host "Main preset-management responsibility leaks found; use focused MainPageViewModel.PresetManagement.*.cs files:" -ForegroundColor Red
    foreach ($leak in $mainPresetManagementResponsibilityLeaks) {
        Write-Host " - $leak" -ForegroundColor Red
    }

    exit 1
}

if ($mainViewModelMonolithicPresetsFiles.Count -gt 0) {
    Write-Host "Monolithic MainPageViewModel Presets files found; use focused MainPageViewModel.Presets.*.cs files:" -ForegroundColor Red
    foreach ($file in $mainViewModelMonolithicPresetsFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($mainViewModelMonolithicEditorFiles.Count -gt 0) {
    Write-Host "Monolithic MainPageViewModel Editor files found; use focused MainPageViewModel.Editor.*.cs files:" -ForegroundColor Red
    foreach ($file in $mainViewModelMonolithicEditorFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($placementTextCatalogLeaks.Count -gt 0) {
    Write-Host "Placement text catalog leaks found; keep fit/anchor/offset copy in LocalizedText.Catalog.cs:" -ForegroundColor Red
    foreach ($leak in $placementTextCatalogLeaks) {
        Write-Host " - $leak" -ForegroundColor Red
    }

    exit 1
}

if ($localizedTextMonolithicCatalogMembers.Count -gt 0) {
    Write-Host "LocalizedText catalog language members found in base catalog; use LocalizedText.Catalog.English.cs and .Spanish.cs:" -ForegroundColor Red
    foreach ($member in $localizedTextMonolithicCatalogMembers) {
        Write-Host " - $member" -ForegroundColor Red
    }

    exit 1
}

if ($localizedTextUnnamedCatalogArgs.Count -gt 0) {
    Write-Host "Unnamed LocalizedText catalog arguments found; use named arguments so constructor order changes stay reviewable:" -ForegroundColor Red
    foreach ($arg in $localizedTextUnnamedCatalogArgs) {
        Write-Host " - $arg" -ForegroundColor Red
    }

    exit 1
}

if ($appDataRootWithoutValidation.Count -gt 0) {
    Write-Host "App data root helpers without blank-path validation found; validate local app-data root before composing Waller paths:" -ForegroundColor Red
    foreach ($file in $appDataRootWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($appCompositionWithoutValidation.Count -gt 0) {
    Write-Host "App composition constructors without null validation found; validate services/stores before MainPageViewModel startup:" -ForegroundColor Red
    foreach ($contract in $appCompositionWithoutValidation) {
        Write-Host " - $contract" -ForegroundColor Red
    }

    exit 1
}

if ($appCurrentSessionLoaderFiles.Count -gt 0) {
    Write-Host "App current-session loader files found; keep monitor detection/fallback policy in Core CurrentSessionLoader:" -ForegroundColor Red
    foreach ($file in $appCurrentSessionLoaderFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($presetMenuItemWithoutNameValidation.Count -gt 0) {
    Write-Host "Preset menu item name validation missing; reject blank names before picker/list surfaces render invisible choices:" -ForegroundColor Red
    foreach ($file in $presetMenuItemWithoutNameValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($sourceSelectionDtoWithoutValidation.Count -gt 0) {
    Write-Host "Source-selection DTO validation missing; normalize picker paths and swatch colors before editor fields mutate session state:" -ForegroundColor Red
    foreach ($file in $sourceSelectionDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($optionDtoWithoutValidation.Count -gt 0) {
    Write-Host "Option DTO validation missing; reject invisible option labels and invalid swatches before XAML/editor surfaces consume them:" -ForegroundColor Red
    foreach ($file in $optionDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($presetSessionDtoWithoutValidation.Count -gt 0) {
    Write-Host "Preset session DTO validation missing; keep save/load/rename session transitions complete before Preset view-model split:" -ForegroundColor Red
    foreach ($file in $presetSessionDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($settingsDtoWithoutValidation.Count -gt 0) {
    Write-Host "Settings DTO validation missing; reject unsupported settings before modal save/load mutates local state:" -ForegroundColor Red
    foreach ($file in $settingsDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($managedPresetCommandDtoWithoutValidation.Count -gt 0) {
    Write-Host "Managed Preset command DTO validation missing; reject invalid ids/names before modal commands mutate Presets:" -ForegroundColor Red
    foreach ($file in $managedPresetCommandDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($workflowResultDtoWithoutValidation.Count -gt 0) {
    Write-Host "Workflow result DTO validation missing; reject impossible Apply/editor result states before UI consumes them:" -ForegroundColor Red
    foreach ($file in $workflowResultDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($editorDtoWithoutValidation.Count -gt 0) {
    Write-Host "Editor DTO validation missing; reject invalid editor drafts/results before MonitorEditViewModel split:" -ForegroundColor Red
    foreach ($file in $editorDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($presetMenuDtoWithoutValidation.Count -gt 0) {
    Write-Host "Preset menu/localized surface validation missing; reject invalid Preset menu refresh and localization inputs before UI selection changes:" -ForegroundColor Red
    foreach ($file in $presetMenuDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($surfaceProjectionDtoWithoutValidation.Count -gt 0) {
    Write-Host "Surface projection DTO validation missing; keep option and monitor-row projections explicit before view-model splits:" -ForegroundColor Red
    foreach ($file in $surfaceProjectionDtoWithoutValidation) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

if ($appDefinedEnumHelperFiles.Count -gt 0) {
    Write-Host "Duplicate App enum helper found; use Waller.Native.Core.Models.DefinedEnumValue for App and Core enum boundaries:" -ForegroundColor Red
    foreach ($file in $appDefinedEnumHelperFiles) {
        Write-Host " - $file" -ForegroundColor Red
    }

    exit 1
}

Write-Host "WinUI code guards passed."
