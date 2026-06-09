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

$loadedAsyncHandlers = @()
$hardCodedStatusText = @()
$rawEnumFallbacks = @()
$directLocalWriteRecovery = @()
$localWriteRecoveryAllowList = @(
    "Waller.Native.App\Platform\LocalDataWriteGuard.cs",
    "Waller.Native.App\ViewModels\ManagedPresetMutation.cs"
)

foreach ($file in Get-ChildItem -LiteralPath $resolvedPath -Recurse -Filter *.cs) {
    if ($file.FullName -match "\\(bin|obj)\\") {
        continue
    }

    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        if ($line -match 'Loaded\s*\+=\s*async\b') {
            $relativePath = Get-NativeRelativePath $file.FullName
            $loadedAsyncHandlers += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '\b(StatusText|ApplyProgressText)\s*=\s*"') {
            $relativePath = Get-NativeRelativePath $file.FullName
            $hardCodedStatusText += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match '_\s*=>\s*.*\.ToString\(\)') {
            $relativePath = Get-NativeRelativePath $file.FullName
            $rawEnumFallbacks += "${relativePath}:$lineNumber`: $($line.Trim())"
        }

        if ($line -match 'LocalDataWriteGuard\.IsRecoverable\s*\(') {
            $relativePath = Get-NativeRelativePath $file.FullName
            if ($localWriteRecoveryAllowList -notcontains $relativePath) {
                $directLocalWriteRecovery += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
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

Write-Host "WinUI code guards passed."
