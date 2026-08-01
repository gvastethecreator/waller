param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [string]$OutputPath = ".\artifacts\signing\devcert.pfx",
    [int]$ValidDays = 365
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot

function Assert-LastExitCode {
    param([string]$Step)

    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

Push-Location $nativeRoot
try {
    . .\scripts\FindWinApp.ps1
    $winappPath = Find-WinApp -RuntimeIdentifier "win-$Platform"
    if (-not $winappPath) {
        throw "winapp CLI not found in PATH or NuGet package cache."
    }

    $outputDirectory = Split-Path -Parent $OutputPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    & $winappPath cert generate `
        --manifest $ManifestPath `
        --output $OutputPath `
        --valid-days $ValidDays `
        --if-exists Skip `
        --export-cer
    Assert-LastExitCode "Dev certificate generation"

    & $winappPath cert info $OutputPath
    Assert-LastExitCode "Dev certificate inspection"

    $resolvedOutputPath = (Resolve-Path $OutputPath).Path
    Write-Host ""
    Write-Host "DEV CERT READY: $resolvedOutputPath" -ForegroundColor Green
    Write-Host "Install trust separately from elevated terminal if needed:" -ForegroundColor Yellow
    Write-Host "$winappPath cert install $resolvedOutputPath" -ForegroundColor Yellow
}
finally {
    Pop-Location
}
