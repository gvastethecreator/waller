param(
    [string]$PackageName,
    [string]$PackagePath,
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"
$nativeRoot = Get-WallerNativeRoot

Push-Location $nativeRoot
try {
    $identity = Get-WallerPackageIdentity `
        -PackageName $PackageName `
        -PackagePath $PackagePath `
        -ManifestPath $ManifestPath
    $PackageName = $identity.Name

    $packages = @(Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue)

    if ($packages.Count -eq 0) {
        Write-Host "DEV PACKAGE NOT INSTALLED: $PackageName" -ForegroundColor Green
        return
    }

    $packages | Select-Object Name, PackageFullName, Publisher, InstallLocation | Format-List | Out-String | Write-Host

    if (-not $Uninstall) {
        Write-Host "UNINSTALL PREFLIGHT FOUND PACKAGE. Re-run with -Uninstall to remove it." -ForegroundColor Yellow
        exit 2
    }

    foreach ($package in $packages) {
        Remove-AppxPackage -Package $package.PackageFullName
        Write-Host "DEV PACKAGE REMOVED: $($package.PackageFullName)" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
