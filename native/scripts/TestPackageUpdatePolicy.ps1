param(
    [string]$ScriptsPath = ".\scripts",
    [string]$PackagingDocPath = ".\docs\PACKAGING.md"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$scriptsFullPath = if ([System.IO.Path]::IsPathRooted($ScriptsPath)) {
    $ScriptsPath
}
else {
    Join-Path $nativeRoot $ScriptsPath
}

$packagingDocFullPath = if ([System.IO.Path]::IsPathRooted($PackagingDocPath)) {
    $PackagingDocPath
}
else {
    Join-Path $nativeRoot $PackagingDocPath
}

$setVersionPath = Join-Path $scriptsFullPath "SetPackageVersion.ps1"

foreach ($path in @($scriptsFullPath, $packagingDocFullPath, $setVersionPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Package update policy input not found: $path"
    }
}

$setVersion = Get-Content -LiteralPath $setVersionPath -Raw
$packagingDoc = Get-Content -LiteralPath $packagingDocFullPath -Raw
$errors = @()

if ($setVersion -notmatch '\$identity\.Version\s*=\s*\$Version') {
    $errors += "SetPackageVersion.ps1 must update only Identity.Version."
}

foreach ($assignment in @("Name", "Publisher")) {
    if ($setVersion -match "\$identity\.$assignment\s*=") {
        $errors += "SetPackageVersion.ps1 must not modify Identity.$assignment."
    }
}

foreach ($term in @(
    "Update Policy",
    "SetPackageVersion.ps1 changes only Identity.Version",
    "LocalCache\Local\Waller",
    "package identity",
    "Presets/settings")) {
    if ($packagingDoc -notmatch [regex]::Escape($term)) {
        $errors += "PACKAGING.md missing update policy term: $term"
    }
}

if ($errors.Count -gt 0) {
    foreach ($error in $errors) {
        Write-Host "PACKAGE UPDATE POLICY ERROR: $error" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Package update policy guard passed."
