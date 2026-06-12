param(
    [switch]$SkipSmoke,
    [switch]$Release,
    [switch]$Package,
    [switch]$DisableNuGetAudit,
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$projectPath = ".\Waller.Native.App\Waller.Native.App.csproj"

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    $global:LASTEXITCODE = 0
    & $Command
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Step failed with exit code $LASTEXITCODE`: $Name"
    }
}

Push-Location $nativeRoot
try {
    $nugetAuditArg = if ($DisableNuGetAudit) { "-p:NuGetAudit=false" } else { $null }
    $buildAndRunAuditArg = if ($DisableNuGetAudit) { "-DisableNuGetAudit" } else { $null }

    Invoke-Step "XAML accessibility lint" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestXamlAccessibility.ps1
    }

    Invoke-Step "XAML localization lint" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestXamlLocalization.ps1
    }

    Invoke-Step "Modal keyboard contract" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestModalKeyboardContract.ps1
    }

    Invoke-Step "Shell command contract" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestShellCommandContract.ps1
    }

    Invoke-Step "WinUI code guards" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestWinUICodeGuards.ps1
    }

    Invoke-Step "Core code guards" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestCoreCodeGuards.ps1
    }

    Invoke-Step "JSON code guards" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestJsonCodeGuards.ps1
    }

    Invoke-Step "Local data policy guard" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestLocalDataPolicy.ps1
    }

    Invoke-Step "Error text code guards" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestErrorTextCodeGuards.ps1
    }

    Invoke-Step "MVP scope guards" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestMvpScopeGuards.ps1
    }

    Invoke-Step "Package asset lint" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestPackageAssets.ps1
    }

    Invoke-Step "Launch contract guard" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestLaunchContract.ps1
    }

    Invoke-Step "Package update policy guard" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestPackageUpdatePolicy.ps1
    }

    Invoke-Step "Signing policy guard" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestSigningPolicy.ps1
    }

    Invoke-Step "Package script guards" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestPackageScriptGuards.ps1
    }

    Invoke-Step "Package diagnostic behavior" {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestPackageDiagnosticBehavior.ps1
    }

    Invoke-Step "Build solution" {
        if ($nugetAuditArg) {
            dotnet build .\Waller.Native.slnx $nugetAuditArg
        }
        else {
            dotnet build .\Waller.Native.slnx
        }
    }

    if ($SkipSmoke) {
        Invoke-Step "Build packaged app" {
            $stepArgs = @($projectPath, "-SkipRun")
            if ($buildAndRunAuditArg) { $stepArgs += $buildAndRunAuditArg }
            powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 @stepArgs
        }
    }

    Invoke-Step "Run tests" {
        if ($nugetAuditArg) {
            dotnet test .\Waller.Native.Tests\Waller.Native.Tests.csproj $nugetAuditArg
        }
        else {
            dotnet test .\Waller.Native.Tests\Waller.Native.Tests.csproj
        }
    }

    if (-not $SkipSmoke) {
        Invoke-Step "Packaged launch smoke" {
            $stepArgs = @("-ProjectPath", $projectPath)
            if ($DisableNuGetAudit) { $stepArgs += "-DisableNuGetAudit" }
            powershell -ExecutionPolicy Bypass -File .\scripts\SmokeLaunch.ps1 @stepArgs
        }
    }

    if ($Release -and -not $Package) {
        Invoke-Step "Release build" {
            $stepArgs = @("-Platform", $Platform, "-ProjectPath", $projectPath)
            if ($DisableNuGetAudit) { $stepArgs += "-DisableNuGetAudit" }
            powershell -ExecutionPolicy Bypass -File .\scripts\BuildRelease.ps1 @stepArgs
        }
    }

    if ($Package) {
        Invoke-Step "Development MSIX package" {
            $stepArgs = @("-Platform", $Platform, "-ProjectPath", $projectPath)
            if ($DisableNuGetAudit) { $stepArgs += "-DisableNuGetAudit" }
            powershell -ExecutionPolicy Bypass -File .\scripts\PackageDevMsix.ps1 @stepArgs
        }
    }

    Write-Host ""
    Write-Host "VERIFY PASSED" -ForegroundColor Green
}
finally {
    Pop-Location
}
