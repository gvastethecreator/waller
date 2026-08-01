param(
    [string]$AppPath = ".\Waller.Native.App",
    [string]$WorkflowPath = ".\Waller.Native.Workflows\Apply"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedAppPath = Join-Path $nativeRoot $AppPath
$resolvedWorkflowPath = Join-Path $nativeRoot $WorkflowPath

if (-not (Test-Path -LiteralPath $resolvedAppPath)) {
    throw "App path not found: $resolvedAppPath"
}

if (-not (Test-Path -LiteralPath $resolvedWorkflowPath)) {
    throw "Apply workflow path not found: $resolvedWorkflowPath"
}

$composition = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Platform\WallerAppComposition.cs") -Raw
$mainViewModel = Get-Content -LiteralPath (Join-Path $resolvedAppPath "ViewModels\MainPageViewModel.cs") -Raw
$applyViewModel = Get-Content -LiteralPath (Join-Path $resolvedAppPath "ViewModels\ApplyViewModel.cs") -Raw
$shellHeader = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\ShellHeader.xaml.cs") -Raw
$shellHeaderXaml = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\ShellHeader.xaml") -Raw
$statusFooter = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\StatusFooter.xaml.cs") -Raw
$statusFooterXaml = Get-Content -LiteralPath (Join-Path $resolvedAppPath "Controls\StatusFooter.xaml") -Raw
$workflow = @(
    Get-ChildItem -LiteralPath $resolvedWorkflowPath -Filter *.cs |
        Sort-Object FullName |
        Get-Content -Raw
) -join "`n"
$errors = @()

if (([regex]::Matches($composition, 'new\s+ApplyWorkflow\s*\(\s*applyService\s*,\s*workspace\s*\)')).Count -ne 1) {
    $errors += "Process composition must create exactly one ApplyWorkflow over the shared workspace."
}

if ($workflow -match 'LocalizedText|ApplyTextPresenter|Microsoft\.UI|Windows\.UI') {
    $errors += "ApplyWorkflow must remain free of localized copy and WinUI types."
}

if ($mainViewModel -notmatch 'public\s+ApplyViewModel\s+Apply') {
    $errors += "MainPageViewModel must expose the focused ApplyViewModel."
}

if (([regex]::Matches($applyViewModel, 'workspace\.ReplaceActiveSession\s*\(')).Count -ne 1) {
    $errors += "ApplyViewModel must replace Active Session through one centralized result projection."
}

if ($shellHeader -notmatch 'typeof\(ApplyViewModel\)' -or
    $statusFooter -notmatch 'typeof\(ApplyViewModel\)') {
    $errors += "Apply controls must declare an ApplyViewModel dependency property."
}

foreach ($binding in @(
    "Apply.ApplyAllCommand",
    "Apply.CanStartApply",
    "Apply.CancelApplyCommand",
    "Apply.ProgressText",
    "Apply.IsApplying")) {
    if ($shellHeaderXaml -notmatch [regex]::Escape($binding) -and
        $statusFooterXaml -notmatch [regex]::Escape($binding)) {
        $errors += "Apply controls missing focused binding: $binding."
    }
}

$allowedApplyServicePath = [System.IO.Path]::GetFullPath(
    (Join-Path $resolvedAppPath "Platform\WallerAppComposition.cs"))
$directServiceReferences = @(
    Get-ChildItem -LiteralPath $resolvedAppPath -Recurse -Filter *.cs |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.FullName -ne $allowedApplyServicePath } |
        Select-String -Pattern '\bWallpaperApplyService\b'
)
if ($directServiceReferences.Count -gt 0) {
    $errors += "App code must access WallpaperApplyService only through ApplyWorkflow."
}

$legacyFiles = @(
    "ApplyRunRequest.cs",
    "ApplyRunUiState.cs",
    "MainPageViewModel.Apply.cs",
    "MainPageViewModel.State.Apply.cs"
)
foreach ($legacyFile in $legacyFiles) {
    if (Test-Path -LiteralPath (Join-Path $resolvedAppPath "ViewModels\$legacyFile")) {
        $errors += "Legacy Apply controller remains: $legacyFile."
    }
}

if ($errors.Count -gt 0) {
    foreach ($applyError in $errors) {
        Write-Host "APPLY WORKFLOW ERROR: $applyError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Apply workflow guard passed."
