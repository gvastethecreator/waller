param(
    [string]$PackagePath = ".\artifacts\packages\Waller-dev-x64.msix",
    [string]$ExpectedPublisher = "CN=Waller",
    [string]$ExpectedDisplayName = "Waller",
    [switch]$SkipTrustCheck
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot

Push-Location $nativeRoot
try {
    . .\scripts\PackageManifest.ps1

    [xml]$manifest = Read-WallerMsixManifest -PackagePath $PackagePath

    $identity = $manifest.Package.Identity
    $properties = $manifest.Package.Properties
    $visuals = $manifest.Package.Applications.Application.VisualElements

    if ($identity.Publisher -ne $ExpectedPublisher) {
        throw "Unexpected publisher: $($identity.Publisher)."
    }

    if ($properties.DisplayName -ne $ExpectedDisplayName) {
        throw "Unexpected package display name: $($properties.DisplayName)."
    }

    if ($visuals.DisplayName -ne $ExpectedDisplayName) {
        throw "Unexpected app display name: $($visuals.DisplayName)."
    }

    if (-not (Test-WallerMsixVersion -Value $identity.Version)) {
        throw "Unexpected package version format: $($identity.Version)."
    }

    $signature = Get-AuthenticodeSignature $PackagePath
    if (-not $signature.SignerCertificate) {
        throw "MSIX package is not signed."
    }

    if ($signature.SignerCertificate.Subject -ne $ExpectedPublisher) {
        throw "Unexpected signing certificate subject: $($signature.SignerCertificate.Subject)."
    }

    Write-Host "MSIX INSPECTION PASSED" -ForegroundColor Green
    Write-Host "Package: $((Get-Item $PackagePath).FullName)"
    Write-Host "Name: $($identity.Name)"
    Write-Host "Publisher: $($identity.Publisher)"
    Write-Host "Version: $($identity.Version)"
    Write-Host "Architecture: $($identity.ProcessorArchitecture)"
    Write-Host "Signature: $($signature.Status) / $($signature.SignerCertificate.Thumbprint)"

    if (-not $SkipTrustCheck) {
        powershell -ExecutionPolicy Bypass -File .\scripts\TestDevCertificateTrust.ps1
        if ($LASTEXITCODE -notin 0, 2) {
            throw "Dev certificate trust check failed."
        }
    }
}
finally {
    Pop-Location
}
