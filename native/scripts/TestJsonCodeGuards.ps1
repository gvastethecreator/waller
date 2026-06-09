param(
    [string[]]$Paths = @(".\Waller.Native.Core", ".\Waller.Native.App")
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$violations = @()

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

        if ($file.Name -eq "LocalJsonFile.cs") {
            continue
        }

        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $file.FullName) {
            $lineNumber++
            if ($line -match 'JsonSerializer\.(Serialize|SerializeAsync|Deserialize|DeserializeAsync)\s*\(') {
                $relativePath = [System.IO.Path]::GetRelativePath($nativeRoot, $file.FullName)
                $violations += "${relativePath}:$lineNumber`: $($line.Trim())"
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Direct JsonSerializer calls found; use LocalJsonFile with WallerJsonContext metadata:" -ForegroundColor Red
    foreach ($violation in $violations) {
        Write-Host " - $violation" -ForegroundColor Red
    }

    exit 1
}

Write-Host "JSON code guards passed."
