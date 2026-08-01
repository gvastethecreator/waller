param(
    [string]$AppPath = ".\Waller.Native.App",
    [string]$WorkflowPath = ".\Waller.Native.Workflows\MonitorEditing"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedAppPath = Join-Path $nativeRoot $AppPath
$resolvedWorkflowPath = Join-Path $nativeRoot $WorkflowPath

if (-not (Test-Path -LiteralPath $resolvedAppPath)) {
    throw "App path not found: $resolvedAppPath"
}

if (-not (Test-Path -LiteralPath $resolvedWorkflowPath)) {
    throw "Monitor editor workflow not found: $resolvedWorkflowPath"
}

$composition = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Platform\WallerAppComposition.cs") -Raw
$mainViewModel = Get-Content -LiteralPath (Join-Path $resolvedAppPath "ViewModels\MainPageViewModel.cs") -Raw
$editorViewModel = @(
    Get-ChildItem -LiteralPath (Join-Path $resolvedAppPath "ViewModels") -Filter "MonitorEditorViewModel*.cs" |
        Sort-Object FullName |
        Get-Content -Raw
) -join "`n"
$workflow = @(
    Get-ChildItem -LiteralPath $resolvedWorkflowPath -Filter *.cs |
        Sort-Object FullName |
        Get-Content -Raw
) -join "`n"
$editPanel = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\EditPanel.xaml.cs") -Raw
$monitorWorkspace = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\MonitorWorkspace.xaml") -Raw
$errors = @()

if (([regex]::Matches($composition, 'new\s+MonitorEditorWorkflow\s*\(\s*\)')).Count -ne 1) {
    $errors += "Process composition must create exactly one MonitorEditorWorkflow."
}

if ($workflow -match 'Microsoft\.UI|Windows\.UI|WallpaperApplyService|DesktopWallpaper|PresetStore') {
    $errors += "MonitorEditorWorkflow must remain free of WinUI, Apply, and Preset persistence dependencies."
}

if ($mainViewModel -notmatch 'public\s+MonitorEditorViewModel\s+Editor') {
    $errors += "MainPageViewModel must expose the focused MonitorEditorViewModel."
}

if ($editPanel -notmatch 'typeof\(MonitorEditorViewModel\)' -or
    $editPanel -match 'typeof\(MainPageViewModel\)') {
    $errors += "EditPanel must depend on MonitorEditorViewModel only."
}

foreach ($binding in @(
    "ViewModel.Editor.SelectedMonitor",
    "ViewModel.Editor.ReassignMissingMonitorCommand",
    "ViewModel.Editor.ForgetMissingMonitorCommand",
    "ViewModel.Editor.CanEditMonitorAssignment",
    "ViewModel.Editor")) {
    if ($monitorWorkspace -notmatch [regex]::Escape($binding)) {
        $errors += "MonitorWorkspace missing focused editor binding: $binding."
    }
}

if (([regex]::Matches($editorViewModel, 'workspace\.ReplaceActiveSession\s*\(')).Count -ne 1) {
    $errors += "MonitorEditorViewModel must replace Active Session through one centralized operation."
}

$legacyFiles = @(
    "DisconnectedMonitorEdit.cs",
    "EditorOffsetPercent.cs",
    "MainPageViewModel.Changes.Editor.cs",
    "MainPageViewModel.Editor.Assignment.cs",
    "MainPageViewModel.Editor.Disconnected.cs",
    "MainPageViewModel.Editor.Options.cs",
    "MainPageViewModel.Editor.Placement.cs",
    "MainPageViewModel.Editor.Selection.cs",
    "MainPageViewModel.Editor.Source.cs",
    "MainPageViewModel.State.Editor.cs",
    "MainPageViewModel.Surface.Editor.cs",
    "MonitorAssignmentUpdate.cs",
    "MonitorEditDraft.cs"
)
foreach ($legacyFile in $legacyFiles) {
    if (Test-Path -LiteralPath (Join-Path $resolvedAppPath "ViewModels\$legacyFile")) {
        $errors += "Legacy monitor editor helper remains: $legacyFile."
    }
}

if ($errors.Count -gt 0) {
    foreach ($editorError in $errors) {
        Write-Host "MONITOR EDITOR WORKFLOW ERROR: $editorError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Monitor editor workflow guard passed."
