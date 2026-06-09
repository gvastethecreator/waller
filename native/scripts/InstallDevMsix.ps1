param(
    [string]$PackagePath = ".\artifacts\packages\Waller-dev-x64.msix",
    [string]$CertificatePath = ".\artifacts\signing\devcert.pfx",
    [switch]$SkipRegistrationCheck,
    [switch]$AllUsersRegistrationCheck,
    [switch]$Install
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot

Push-Location $nativeRoot
try {
    if (-not (Test-Path $PackagePath)) {
        throw "MSIX package not found: $PackagePath"
    }

    powershell -ExecutionPolicy Bypass -File .\scripts\InspectDevMsix.ps1 -PackagePath $PackagePath -SkipTrustCheck

    if (-not $SkipRegistrationCheck) {
        $registrationArgs = @("-PackagePath", $PackagePath)
        if ($AllUsersRegistrationCheck) {
            $registrationArgs += "-AllUsers"
        }

        powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 @registrationArgs
        if ($LASTEXITCODE -eq 2) {
            Write-Host "INSTALL PREFLIGHT BLOCKED: development package is already registered for the current user." -ForegroundColor Yellow
            Write-Host "Run scripts\UninstallDevPackage.ps1 first, then re-run install preflight." -ForegroundColor Yellow
            exit 2
        }

        if ($LASTEXITCODE -eq 3) {
            Write-Host "INSTALL PREFLIGHT BLOCKED: all-user package registration check was inconclusive." -ForegroundColor Yellow
            Write-Host "Re-run from an elevated terminal or omit -AllUsersRegistrationCheck for a current-user-only preflight." -ForegroundColor Yellow
            exit 3
        }

        if ($LASTEXITCODE -ne 0) {
            throw "Development package registration check failed."
        }
    }

    powershell -ExecutionPolicy Bypass -File .\scripts\TestDevCertificateTrust.ps1 -CertificatePath $CertificatePath
    if ($LASTEXITCODE -eq 2) {
        Write-Host "INSTALL PREFLIGHT BLOCKED: development certificate is not trusted." -ForegroundColor Yellow
        Write-Host "Install it from an elevated terminal before installing the MSIX." -ForegroundColor Yellow
        exit 2
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Development certificate trust check failed."
    }

    if (-not $Install) {
        Write-Host "INSTALL PREFLIGHT PASSED. Re-run with -Install to call Add-AppxPackage." -ForegroundColor Green
        return
    }

    Add-AppxPackage -Path $PackagePath
    Write-Host "DEV MSIX INSTALLED: $PackagePath" -ForegroundColor Green
}
finally {
    Pop-Location
}
