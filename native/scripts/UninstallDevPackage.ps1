param(
    [string]$PackageName,
    [string]$PackagePath,
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [switch]$AllUsers,
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"
. "$PSScriptRoot\PackageRegistration.ps1"
$nativeRoot = Get-WallerNativeRoot

Push-Location $nativeRoot
try {
    $identity = Get-WallerPackageIdentity `
        -PackageName $PackageName `
        -PackagePath $PackagePath `
        -ManifestPath $ManifestPath
    $PackageName = $identity.Name

    $packages = if ($AllUsers) {
        try {
            @(Get-WallerAllUserPackageRegistrations -PackageName $PackageName)
        }
        catch {
            Write-Host "ALL USERS PACKAGE CHECK FAILED: $($_.Exception.Message)" -ForegroundColor Yellow
            Write-Host "Run this script from an elevated terminal for all-user cleanup preflight." -ForegroundColor Yellow
            Write-WallerPackageConflictHelp -PackageName $PackageName
            exit 3
        }
    }
    else {
        @(Get-WallerCurrentUserPackageRegistrations -PackageName $PackageName)
    }

    if ($packages.Count -eq 0) {
        $scope = if ($AllUsers) { "ALL USERS" } else { "CURRENT USER" }
        Write-Host "DEV PACKAGE NOT INSTALLED FOR ${scope}: $PackageName" -ForegroundColor Green
        return
    }

    Write-WallerPackageRegistrations -Packages $packages -AllUsers:$AllUsers

    if (-not $Uninstall) {
        $scopeFlag = if ($AllUsers) { " -AllUsers" } else { "" }
        Write-Host "UNINSTALL PREFLIGHT FOUND PACKAGE. Re-run with$scopeFlag -Uninstall to remove it intentionally." -ForegroundColor Yellow
        Write-WallerPackageConflictHelp -PackageName $PackageName
        exit 2
    }

    foreach ($package in $packages) {
        if ($AllUsers) {
            Remove-AppxPackage -Package $package.PackageFullName -AllUsers
        }
        else {
            Remove-AppxPackage -Package $package.PackageFullName
        }

        Write-Host "DEV PACKAGE REMOVED: $($package.PackageFullName)" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
