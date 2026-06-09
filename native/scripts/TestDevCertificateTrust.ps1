param(
    [string]$CertificatePath = ".\artifacts\signing\devcert.pfx",
    [string]$Password = "password"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot

Push-Location $nativeRoot
try {
    if (-not (Test-Path $CertificatePath)) {
        throw "Certificate not found: $CertificatePath"
    }

    . .\scripts\FindWinApp.ps1
    $winappPath = Find-WinApp
    if (-not $winappPath) {
        throw "winapp CLI not found in PATH or NuGet package cache."
    }

    $resolvedCertificatePath = (Resolve-Path $CertificatePath).Path
    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
        $resolvedCertificatePath,
        $Password)

    $thumbprint = $certificate.Thumbprint
    $matches = Get-ChildItem Cert:\CurrentUser\Root, Cert:\LocalMachine\Root -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $thumbprint }

    if ($matches) {
        Write-Host "DEV CERT TRUSTED: $thumbprint" -ForegroundColor Green
        $matches | Select-Object Subject, Thumbprint, PSParentPath | Format-List | Out-String | Write-Host
        exit 0
    }

    Write-Host "DEV CERT NOT TRUSTED: $thumbprint" -ForegroundColor Yellow
    Write-Host "Install from elevated terminal if MSIX install is needed:" -ForegroundColor Yellow
    Write-Host "$winappPath cert install $resolvedCertificatePath" -ForegroundColor Yellow
    exit 2
}
finally {
    Pop-Location
}
