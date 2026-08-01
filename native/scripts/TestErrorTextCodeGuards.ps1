param(
    [string[]]$Paths = @(".\Waller.Native.Core", ".\Waller.Native.App")
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$rawExceptionMessages = @()
$interpolatedApplyResultMessages = @()
$genericApplyErrorFallbacks = @()

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

    foreach ($file in Get-ChildItem -LiteralPath $resolvedPath -Recurse -Filter *.cs) {
        if ($file.FullName -match "\\(bin|obj)\\") {
            continue
        }

        $relativePath = Get-NativeRelativePath $file.FullName
        $lines = @(Get-Content -LiteralPath $file.FullName)
        $lineNumber = 0
        foreach ($line in $lines) {
            $lineNumber++
            if ($line -match '\b(error|exception|ex)\.Message\b') {
                $rawExceptionMessages += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }

        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -notmatch 'ApplyResult\.Failure\s*\(') {
                continue
            }

            $endIndex = [Math]::Min($index + 8, $lines.Count - 1)
            $callWindow = ($lines[$index..$endIndex] -join "`n")
            if ($callWindow -match '\$"') {
                $lineNumber = $index + 1
                $interpolatedApplyResultMessages += "${relativePath}:$lineNumber`: $($lines[$index].Trim())"
            }
        }

        if ($relativePath -eq "Waller.Native.App\ViewModels\LocalizedText.Apply.cs") {
            $insideApplyErrorLabel = $false
            for ($index = 0; $index -lt $lines.Count; $index++) {
                if ($lines[$index] -match '\bApplyErrorLabel\s*\(') {
                    $insideApplyErrorLabel = $true
                }

                if ($insideApplyErrorLabel -and $lines[$index] -match '=>\s*(CheckValue|applyError)\s*,') {
                    $lineNumber = $index + 1
                    $genericApplyErrorFallbacks += "${relativePath}:$lineNumber`: $($lines[$index].Trim())"
                }

                if ($insideApplyErrorLabel -and $lines[$index] -match '^\s*};\s*$') {
                    $insideApplyErrorLabel = $false
                }
            }
        }
    }
}

if ($rawExceptionMessages.Count -gt 0) {
    Write-Host "Raw exception Message usage found; map errors to stable codes or localized presenters:" -ForegroundColor Red
    foreach ($message in $rawExceptionMessages) {
        Write-Host " - $message" -ForegroundColor Red
    }

    exit 1
}

if ($interpolatedApplyResultMessages.Count -gt 0) {
    Write-Host "Interpolated ApplyResult failure messages found; use stable error codes or localized presenters:" -ForegroundColor Red
    foreach ($message in $interpolatedApplyResultMessages) {
        Write-Host " - $message" -ForegroundColor Red
    }

    exit 1
}

if ($genericApplyErrorFallbacks.Count -gt 0) {
    Write-Host "Generic/raw Apply error fallbacks found; use localized UnknownApplyError for unknown Apply errors:" -ForegroundColor Red
    foreach ($fallback in $genericApplyErrorFallbacks) {
        Write-Host " - $fallback" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Error text code guards passed."
