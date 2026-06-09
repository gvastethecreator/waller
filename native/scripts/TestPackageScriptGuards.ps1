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
$allowedFiles = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
[void]$allowedFiles.Add("PackageManifest.ps1")

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
    }
}

if ($directManifestReads.Count -gt 0) {
    Write-Host "Direct package manifest reads found; use PackageManifest.ps1 helpers instead:" -ForegroundColor Red
    foreach ($read in $directManifestReads) {
        Write-Host " - $read" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Package script guards passed."
