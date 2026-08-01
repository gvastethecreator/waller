param(
    [string]$PackageName,
    [string]$PackagePath,
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [Alias("AllUsers")]
    [switch]$CheckAllUsers
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"
. "$PSScriptRoot\PackageRegistration.ps1"
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

    if ($CheckAllUsers -and -not (Test-WallerProcessIsElevated)) {
        Write-Host "CURRENT USER PACKAGE CHECK SKIPPED IN COMBINED ALL-USERS MODE." -ForegroundColor Yellow
        Write-Host "Run this script without -AllUsers for a current-user-only preflight." -ForegroundColor Yellow
        Write-Host "ALL USERS PACKAGE CHECK FAILED: All-user package inspection requires an elevated terminal." -ForegroundColor Yellow
        Write-Host "Run this script from an elevated terminal for a conclusive all-user registration check." -ForegroundColor Yellow
        Write-WallerPackageConflictHelp -PackageName $PackageName
        Write-Host "REGISTRATION PREFLIGHT INCONCLUSIVE: all-user registrations could not be checked." -ForegroundColor Yellow
        exit 3
    }

    $packages = @(Get-WallerCurrentUserPackageRegistrations -PackageName $PackageName)
    if ($packages.Count -eq 0) {
        Write-Host "CURRENT USER PACKAGE NOT REGISTERED: $PackageName" -ForegroundColor Green
    }
    else {
        Write-Host "CURRENT USER PACKAGE REGISTERED: $PackageName" -ForegroundColor Yellow
        Write-WallerPackageRegistrations -Packages $packages
    }

    if ($CheckAllUsers) {
        try {
            $allUserPackages = @(Get-WallerAllUserPackageRegistrations -PackageName $PackageName)
            if ($allUserPackages.Count -eq 0) {
                Write-Host "ALL USERS PACKAGE NOT REGISTERED: $PackageName" -ForegroundColor Green
            }
            else {
                Write-Host "ALL USERS PACKAGE REGISTRATIONS:" -ForegroundColor Yellow
                Write-WallerPackageRegistrations -Packages $allUserPackages -AllUsers
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
        Write-WallerPackageConflictHelp -PackageName $PackageName
        exit 2
    }

    if ($allUsersCheckFailed) {
        Write-Host "REGISTRATION PREFLIGHT INCONCLUSIVE: current user is clean, but all-user registrations could not be checked." -ForegroundColor Yellow
        Write-WallerPackageConflictHelp -PackageName $PackageName
        exit 3
    }
}
finally {
    Pop-Location
}
