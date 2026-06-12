param(
    [string[]]$Paths = @(".\Waller.Native.App", ".\Waller.Native.Core")
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$blockedFeatures = @(
    "ImageEditor",
    "Identify",
    "Logs",
    "Import",
    "Export",
    "Plugin",
    "DynamicWallpaper",
    "Scheduler",
    "ScheduledWallpaper",
    "Tray"
)
$violations = @()

function Get-NativeRelativePath {
    param([string]$Path)

    $root = (Resolve-Path -LiteralPath $nativeRoot).Path.TrimEnd("\", "/")
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if ($resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($root.Length).TrimStart("\", "/")
    }

    return $resolved
}

foreach ($path in $Paths) {
    $resolvedPath = if ([System.IO.Path]::IsPathRooted($path)) {
        $path
    }
    else {
        Join-Path $nativeRoot $path
    }

    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "Guard path not found: $resolvedPath"
    }

    foreach ($file in Get-ChildItem -LiteralPath $resolvedPath -Recurse -File) {
        if ($file.Extension -notin @(".cs", ".xaml")) {
            continue
        }

        if ($file.FullName -match "\\(bin|obj)\\") {
            continue
        }

        $relativePath = Get-NativeRelativePath $file.FullName
        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $file.FullName) {
            $lineNumber++
            foreach ($feature in $blockedFeatures) {
                if ($line -match "\b$feature\b") {
                    $violations += "${relativePath}:$lineNumber`: blocked MVP feature '$feature': $($line.Trim())"
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Blocked non-MVP feature hooks found; keep image editing, identify, logs, import/export, plugins, tray, and scheduling out of MVP:" -ForegroundColor Red
    foreach ($violation in $violations) {
        Write-Host " - $violation" -ForegroundColor Red
    }

    exit 1
}

Write-Host "MVP scope guards passed."
