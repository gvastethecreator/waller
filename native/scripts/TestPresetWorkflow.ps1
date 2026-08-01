param(
    [string]$AppPath = ".\Waller.Native.App",
    [string]$WorkflowPath = ".\Waller.Native.Workflows\Presets\PresetWorkflow.cs"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedAppPath = Join-Path $nativeRoot $AppPath
$resolvedWorkflowPath = Join-Path $nativeRoot $WorkflowPath

if (-not (Test-Path -LiteralPath $resolvedAppPath)) {
    throw "App path not found: $resolvedAppPath"
}

if (-not (Test-Path -LiteralPath $resolvedWorkflowPath)) {
    throw "Preset workflow not found: $resolvedWorkflowPath"
}

$composition = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Platform\WallerAppComposition.cs") -Raw
$localState = Get-Content -LiteralPath (Join-Path $resolvedAppPath "ViewModels\MainPageLocalState.cs") -Raw
$workflow = Get-Content -LiteralPath $resolvedWorkflowPath -Raw
$shellHeader = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\ShellHeader.xaml.cs") -Raw
$saveAsModal = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\SaveAsModal.xaml.cs") -Raw
$manageModal = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\ManagePresetsModal.xaml.cs") -Raw
$errors = @()

if (([regex]::Matches($composition, 'new\s+PresetWorkflow\s*\(\s*localData\.Presets\s*\)')).Count -ne 1) {
    $errors += "Process composition must create exactly one PresetWorkflow."
}

if ($workflow -match 'WallpaperApplyService|DesktopWallpaper|ApplyAsync') {
    $errors += "Preset selection must not depend on wallpaper Apply."
}

if ($localState -match '\bPreset(Store|Workflow|Menu|Session)?\b') {
    $errors += "MainPageLocalState must not forward Preset operations."
}

$allowedStorePath = [System.IO.Path]::GetFullPath((Join-Path $resolvedAppPath "Platform\WallerLocalDataStores.cs"))
$directStoreReferences = @(
    Get-ChildItem -LiteralPath $resolvedAppPath -Recurse -Filter *.cs |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.FullName -ne $allowedStorePath } |
        Select-String -Pattern '\bPresetStore\b'
)
if ($directStoreReferences.Count -gt 0) {
    $errors += "App code must access PresetStore only through PresetWorkflow."
}

if ($shellHeader -notmatch 'typeof\(PresetsViewModel\)' -or
    $saveAsModal -notmatch 'typeof\(PresetsViewModel\)' -or
    $manageModal -notmatch 'typeof\(PresetsViewModel\)') {
    $errors += "Preset controls must depend on PresetsViewModel, not the full MainPageViewModel."
}

$legacyHelpers = @(
    "ActivePresetSession.cs",
    "ManagedPresetDelete.cs",
    "ManagedPresetList.cs",
    "ManagedPresetMutation.cs",
    "PresetMenuRefresh.cs",
    "PresetSessionSave.cs",
    "SelectedPresetSession.cs",
    "SelectedPresetSessionLoader.cs"
)
foreach ($legacyHelper in $legacyHelpers) {
    if (Test-Path -LiteralPath (Join-Path $resolvedAppPath "ViewModels\$legacyHelper")) {
        $errors += "Legacy Preset helper remains: $legacyHelper."
    }
}

if ($errors.Count -gt 0) {
    foreach ($presetError in $errors) {
        Write-Host "PRESET WORKFLOW ERROR: $presetError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Preset workflow guard passed."
