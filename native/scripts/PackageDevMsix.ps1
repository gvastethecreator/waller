param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    [string]$CertificatePath = ".\artifacts\signing\devcert.pfx",
    [string]$OutputPath,
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [switch]$DisableNuGetAudit
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$targetFramework = "net10.0-windows10.0.26100.0"
$runtimeIdentifier = "win-$Platform"
$inputFolder = ".\Waller.Native.App\bin\$Platform\Release\$targetFramework\$runtimeIdentifier"

if (-not $OutputPath) {
    $OutputPath = ".\artifacts\packages\Waller-dev-$Platform.msix"
}

function Assert-LastExitCode {
    param([string]$Step)

    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

Push-Location $nativeRoot
try {
    . .\scripts\FindWinApp.ps1
    $winappPath = Find-WinApp -RuntimeIdentifier $runtimeIdentifier
    if (-not $winappPath) {
        throw "winapp CLI not found in PATH or NuGet package cache."
    }

    $releaseArgs = @(
        "-Platform", $Platform,
        "-ProjectPath", $ProjectPath
    )
    if ($DisableNuGetAudit) {
        $releaseArgs += "-DisableNuGetAudit"
    }

    powershell -ExecutionPolicy Bypass -File .\scripts\BuildRelease.ps1 @releaseArgs
    Assert-LastExitCode "Release build"

    if (-not (Test-Path $CertificatePath)) {
        powershell -ExecutionPolicy Bypass -File .\scripts\PrepareDevCertificate.ps1 `
            -Platform $Platform `
            -OutputPath $CertificatePath
        Assert-LastExitCode "Dev certificate generation"
    }

    $outputDirectory = Split-Path -Parent $OutputPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    & $winappPath package `
        $inputFolder `
        --cert $CertificatePath `
        --output $OutputPath
    Assert-LastExitCode "MSIX package creation"

    if (-not (Test-Path $OutputPath)) {
        throw "MSIX package was not created: $OutputPath"
    }

    powershell -ExecutionPolicy Bypass -File .\scripts\InspectDevMsix.ps1 -PackagePath $OutputPath
    Assert-LastExitCode "MSIX inspection"

    $package = Get-Item $OutputPath
    $resolvedCertificatePath = (Resolve-Path $CertificatePath).Path
    Write-Host ""
    Write-Host "DEV MSIX READY: $($package.FullName)" -ForegroundColor Green
    Write-Host "Size: $($package.Length) bytes" -ForegroundColor Green
    Write-Host "Trust cert separately from elevated terminal before installing:" -ForegroundColor Yellow
    Write-Host "$winappPath cert install $resolvedCertificatePath" -ForegroundColor Yellow
}
finally {
    Pop-Location
}
