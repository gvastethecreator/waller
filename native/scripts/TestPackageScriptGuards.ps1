param(
    [string]$ScriptsPath = ".\scripts"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedPath = if ([System.IO.Path]::IsPathRooted($ScriptsPath)) {
    $ScriptsPath
}
else {
    Join-Path $nativeRoot $ScriptsPath
}

if (-not (Test-Path -LiteralPath $resolvedPath)) {
    throw "Scripts path not found: $resolvedPath"
}

$directManifestReads = @()
$unsafePackageRemovals = @()
$unsafePackageInstalls = @()
$directPackageLookups = @()
$allowedFiles = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$allowedFiles.Add("PackageManifest.ps1")
$packageLookupAllowList = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$packageLookupAllowList.Add("PackageRegistration.ps1")
$packageRemovalAllowList = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$packageRemovalAllowList.Add("UninstallDevPackage.ps1")
$packageInstallAllowList = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$packageInstallAllowList.Add("InstallDevMsix.ps1")

function Get-RelativeNativePath {
    param([string]$Path)

    $root = $nativeRoot.TrimEnd("\")
    if ($Path.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($root.Length).TrimStart("\")
    }

    return $Path
}

foreach ($file in Get-ChildItem -LiteralPath $resolvedPath -Recurse -Filter *.ps1) {
    if ($allowedFiles.Contains($file.Name)) {
        continue
    }

    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        if ($line -match '\[xml\]\s*\$manifest\s*=\s*Get-Content' -or $line -match 'Get-Content.*Package\.appxmanifest') {
            $relativePath = Get-RelativeNativePath $file.FullName
            $directManifestReads += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '\bRemove-AppxPackage\b' -and -not $packageRemovalAllowList.Contains($file.Name)) {
            $relativePath = Get-RelativeNativePath $file.FullName
            $unsafePackageRemovals += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '\bAdd-AppxPackage\b' -and -not $packageInstallAllowList.Contains($file.Name)) {
            $relativePath = Get-RelativeNativePath $file.FullName
            $unsafePackageInstalls += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '\bGet-AppxPackage\s' -and -not $packageLookupAllowList.Contains($file.Name)) {
            $relativePath = Get-RelativeNativePath $file.FullName
            $directPackageLookups += "${relativePath}:$lineNumber`: $($line.Trim())"
        }
    }
}

if ($directManifestReads.Count -gt 0) {
    Write-Host "Direct package manifest reads found; use PackageManifest.ps1 helpers instead:" -ForegroundColor Red
    foreach ($read in $directManifestReads) {
        Write-Host " - $read" -ForegroundColor Red
    }

    exit 1
}

if ($unsafePackageRemovals.Count -gt 0) {
    Write-Host "Unsafe package removal commands found; keep removal isolated in UninstallDevPackage.ps1:" -ForegroundColor Red
    foreach ($removal in $unsafePackageRemovals) {
        Write-Host " - $removal" -ForegroundColor Red
    }

    exit 1
}

if ($unsafePackageInstalls.Count -gt 0) {
    Write-Host "Unsafe package install commands found; keep install isolated in InstallDevMsix.ps1:" -ForegroundColor Red
    foreach ($install in $unsafePackageInstalls) {
        Write-Host " - $install" -ForegroundColor Red
    }

    exit 1
}

if ($directPackageLookups.Count -gt 0) {
    Write-Host "Direct package registration lookups found; use PackageRegistration.ps1 helpers instead:" -ForegroundColor Red
    foreach ($lookup in $directPackageLookups) {
        Write-Host " - $lookup" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Package script guards passed."
