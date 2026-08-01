param(
    [string]$AppPath = ".\Waller.Native.App",
    [string]$WorkflowPath = ".\Waller.Native.Workflows\Settings\UserSettingsWorkflow.cs"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedAppPath = Join-Path $nativeRoot $AppPath
$resolvedWorkflowPath = Join-Path $nativeRoot $WorkflowPath

if (-not (Test-Path -LiteralPath $resolvedAppPath)) {
    throw "App path not found: $resolvedAppPath"
}

if (-not (Test-Path -LiteralPath $resolvedWorkflowPath)) {
    throw "UserSettings workflow not found: $resolvedWorkflowPath"
}

$compositionPath = Join-Path $resolvedAppPath "Platform\WallerAppComposition.cs"
$storesPath = Join-Path $resolvedAppPath "Platform\WallerLocalDataStores.cs"
$composition = Get-Content -LiteralPath $compositionPath -Raw
$errors = @()

if (([regex]::Matches($composition, 'new\s+UserSettingsWorkflow\s*\(')).Count -ne 1) {
    $errors += "Process composition must create exactly one UserSettingsWorkflow."
}

if ($composition -notmatch 'new\s+WindowPlacementWorkflow\s*\(\s*userSettings\s*\)' -or
    $composition -notmatch 'new\s+WallerAppServices\([\s\S]*userSettings') {
    $errors += "Window placement and page services must share the composed UserSettingsWorkflow."
}

$allowedStoreReference = [System.IO.Path]::GetFullPath($storesPath)
$directStoreReferences = @(
    Get-ChildItem -LiteralPath $resolvedAppPath -Recurse -Filter *.cs |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.FullName -ne $allowedStoreReference } |
        Select-String -Pattern '\bUserSettingsStore\b'
)
if ($directStoreReferences.Count -gt 0) {
    $errors += "App code must access UserSettingsStore only through the process workflow."
}

if ($errors.Count -gt 0) {
    foreach ($workflowError in $errors) {
        Write-Host "USER SETTINGS WORKFLOW ERROR: $workflowError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "UserSettings workflow guard passed."
