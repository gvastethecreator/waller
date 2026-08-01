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

$inputs = @{
    Paths = Join-Path $resolvedPath "Platform\WallerAppDataPaths.cs"
    Stores = Join-Path $resolvedPath "Platform\WallerLocalDataStores.cs"
    Composition = Join-Path $resolvedPath "Platform\WallerAppComposition.cs"
}

foreach ($path in $inputs.Values) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Local data policy input not found: $path"
    }
}

$pathsText = Get-Content -LiteralPath $inputs.Paths -Raw
$storesText = Get-Content -LiteralPath $inputs.Stores -Raw
$compositionText = Get-Content -LiteralPath $inputs.Composition -Raw
$errors = @()

if ($pathsText -notmatch 'LocalDataLayout\.Create\s*\(' -or
    $pathsText -notmatch 'Environment\.SpecialFolder\.LocalApplicationData' -or
    $pathsText -notmatch 'UserVisibleProfileDirectory\s*\(\s*\)') {
    $errors += "App environment adapter must pass explicit local-app-data and user-profile inputs to LocalDataLayout."
}

if ($pathsText -match 'Package\.Current|ApplicationData\.Current') {
    $errors += "Local data environment reads must not depend on package identity APIs."
}

if ($storesText -notmatch 'Create\s*\(\s*WallerAppDataPaths\.Current\s*\)' -or
    $storesText -notmatch 'PresetStore\s*\(\s*layout\.AppDataRoot\s*\)' -or
    $storesText -notmatch 'UserSettingsStore\s*\(\s*layout\.AppDataRoot\s*\)' -or
    $storesText -notmatch 'RenderedWallpaperStore\s*\(\s*layout\.RenderedCacheRoot\s*\)') {
    $errors += "All local stores must be constructed from one typed LocalDataLayout."
}

if ($compositionText -notmatch 'WallerLocalDataStores\.CreateDefault\s*\(\s*\)') {
    $errors += "Process composition must create the default local-store graph once."
}

if ($errors.Count -gt 0) {
    foreach ($policyError in $errors) {
        Write-Host "LOCAL DATA POLICY ERROR: $policyError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Local data policy guard passed."
