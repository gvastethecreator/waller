param(
    [string]$Version,
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"
$nativeRoot = Get-WallerNativeRoot

Push-Location $nativeRoot
try {
    [xml]$manifest = Read-WallerPackageManifest -ManifestPath $ManifestPath
    $identity = $manifest.Package.Identity

    if (-not $identity) {
        throw "Package manifest has no Identity node: $ManifestPath"
    }

    $currentVersion = $identity.Version

    if (-not $Version) {
        Write-Host "Package name: $($identity.Name)"
        Write-Host "Publisher: $($identity.Publisher)"
        Write-Host "Version: $currentVersion"
        return
    }

    if (-not (Test-WallerMsixVersion -Value $Version)) {
        throw "Invalid MSIX version '$Version'. Use four numeric parts between 0 and 65535, for example 1.2.3.4."
    }

    if ($Version -eq $currentVersion) {
        Write-Host "Package version already $Version"
        return
    }

    $identity.Version = $Version

    $resolvedPath = Resolve-Path $ManifestPath
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.Indent = $true

    $writer = [System.Xml.XmlWriter]::Create($resolvedPath.Path, $settings)
    try {
        $manifest.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    Write-Host "Package version changed: $currentVersion -> $Version" -ForegroundColor Green
    Write-Host "Manifest: $($resolvedPath.Path)"
}
finally {
    Pop-Location
}
