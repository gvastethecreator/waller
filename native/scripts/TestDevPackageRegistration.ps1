param(
    [string]$PackageName,
    [string]$PackagePath,
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [switch]$AllUsers
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"
$nativeRoot = Get-WallerNativeRoot

Push-Location $nativeRoot
try {
    $allUsersCheckFailed = $false

    $identity = Get-WallerPackageIdentity `
        -PackageName $PackageName `
        -PackagePath $PackagePath `
        -ManifestPath $ManifestPath
    $PackageName = $identity.Name

    Write-Host "Package name: $PackageName"
    if ($identity.Publisher) {
        Write-Host "Publisher: $($identity.Publisher)"
    }

    if ($identity.Version) {
        Write-Host "Package version: $($identity.Version)"
    }

    $packages = @(Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue)
    if ($packages.Count -eq 0) {
        Write-Host "CURRENT USER PACKAGE NOT REGISTERED: $PackageName" -ForegroundColor Green
    }
    else {
        Write-Host "CURRENT USER PACKAGE REGISTERED: $PackageName" -ForegroundColor Yellow
        $packages |
            Select-Object Name, PackageFullName, Publisher, InstallLocation |
            Format-List |
            Out-String |
            Write-Host
    }

    if ($AllUsers) {
        try {
            $allUserPackages = @(Get-AppxPackage -AllUsers -Name $PackageName -ErrorAction Stop)
            if ($allUserPackages.Count -eq 0) {
                Write-Host "ALL USERS PACKAGE NOT REGISTERED: $PackageName" -ForegroundColor Green
            }
            else {
                Write-Host "ALL USERS PACKAGE REGISTRATIONS:" -ForegroundColor Yellow
                $allUserPackages |
                    Select-Object Name, PackageFullName, Publisher, PackageUserInformation |
                    Format-List |
                    Out-String |
                    Write-Host
            }
        }
        catch {
            $allUsersCheckFailed = $true
            Write-Host "ALL USERS PACKAGE CHECK FAILED: $($_.Exception.Message)" -ForegroundColor Yellow
            Write-Host "Run this script from an elevated terminal for a conclusive all-user registration check." -ForegroundColor Yellow
        }
    }

    if ($packages.Count -gt 0) {
        Write-Host "REGISTRATION PREFLIGHT FOUND PACKAGE. Smoke launch may fail with package identity conflicts." -ForegroundColor Yellow
        Write-Host "Run scripts\UninstallDevPackage.ps1 -Uninstall only if you intentionally want to remove this dev package." -ForegroundColor Yellow
        exit 2
    }

    if ($allUsersCheckFailed) {
        Write-Host "REGISTRATION PREFLIGHT INCONCLUSIVE: current user is clean, but all-user registrations could not be checked." -ForegroundColor Yellow
        exit 3
    }
}
finally {
    Pop-Location
}
