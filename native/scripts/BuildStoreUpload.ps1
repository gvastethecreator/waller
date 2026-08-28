#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('x64', 'x86', 'ARM64')]
    [string] $Platform = 'x64',

    [Parameter()]
    [ValidateSet('Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [switch] $SkipVerification,

    [Parameter()]
    [string] $PackageCertificateKeyFile = $env:WALLER_STORE_CERTIFICATE_PATH,

    [Parameter()]
    [string] $PackageCertificatePassword = $env:WALLER_STORE_CERTIFICATE_PASSWORD,

    [Parameter()]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$nativeRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $nativeRoot
$projectPath = Join-Path $nativeRoot 'Waller.Native.App\Waller.Native.App.csproj'
$manifestPath = Join-Path $nativeRoot 'Waller.Native.App\Package.appxmanifest'
$readinessScript = Join-Path $PSScriptRoot 'TestStoreReadiness.ps1'
$rootExecutor = Join-Path $repoRoot 'scripts\Invoke-Native.ps1'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $nativeRoot "artifacts\store\$($Platform.ToLowerInvariant())"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Resolve-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
        throw 'Visual Studio Installer vswhere.exe is required.'
    }

    $installation = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($installation)) {
        throw 'A Visual Studio installation with MSBuild is required.'
    }

    $candidate = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "MSBuild was not found at $candidate."
    }

    return $candidate
}

function Assert-Command {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [scriptblock] $Command
    )

    Write-Host "==> $Name" -ForegroundColor Cyan
    $global:LASTEXITCODE = 0
    & $Command
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

function Reset-OutputDirectory {
    param([Parameter(Mandatory)] [string] $Path)

    $artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $nativeRoot 'artifacts')).TrimEnd('\') + '\'
    if (-not $Path.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a Store path outside native/artifacts: $Path"
    }

    if (Test-Path -LiteralPath $Path) {
        [System.IO.Directory]::Delete($Path, $true)
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function New-RandomPassword {
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }
    return [Convert]::ToBase64String($bytes)
}

function New-TemporaryStoreCertificate {
    param([Parameter(Mandatory)] [string] $Publisher)

    $password = New-RandomPassword
    $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
    $temporaryRoot = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
        [System.IO.Path]::GetTempPath()
    }
    else {
        $env:RUNNER_TEMP
    }
    $pfxPath = Join-Path $temporaryRoot "Waller-store-build-$([Guid]::NewGuid().ToString('N')).pfx"

    $certificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $Publisher `
        -FriendlyName 'Waller Store build certificate (temporary)' `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -NotAfter (Get-Date).AddDays(7) `
        -TextExtension '2.5.29.37={text}1.3.6.1.5.5.7.3.3'

    Export-PfxCertificate `
        -Cert $certificate `
        -FilePath $pfxPath `
        -Password $securePassword `
        -Force | Out-Null

    return [ordered]@{
        path = $pfxPath
        password = $password
        thumbprint = $certificate.Thumbprint
        generated = $true
    }
}

& $readinessScript -RequireReservedIdentity
if ($LASTEXITCODE -ne 0) {
    throw 'The reserved Partner Center identity is required before building a Store upload.'
}

[xml] $manifest = Get-Content -LiteralPath $manifestPath -Raw
$publisher = [string] $manifest.Package.Identity.Publisher
$packageName = [string] $manifest.Package.Identity.Name
$packageVersion = [string] $manifest.Package.Identity.Version

$certificateState = $null
$temporaryCertificate = $false
try {
    if ($PackageCertificateKeyFile) {
        $resolvedCertificatePath = (Resolve-Path -LiteralPath $PackageCertificateKeyFile).Path
        if (-not $PackageCertificatePassword) {
            throw 'WALLER_STORE_CERTIFICATE_PASSWORD or -PackageCertificatePassword is required with a supplied PFX.'
        }

        $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $resolvedCertificatePath,
            $PackageCertificatePassword
        )
        try {
            if (-not [string]::Equals($certificate.Subject, $publisher, [StringComparison]::Ordinal)) {
                throw "Certificate subject '$($certificate.Subject)' does not match manifest publisher '$publisher'."
            }
            $certificateState = [ordered]@{
                path = $resolvedCertificatePath
                password = $PackageCertificatePassword
                thumbprint = $certificate.Thumbprint
                generated = $false
            }
        }
        finally {
            $certificate.Dispose()
        }
    }
    else {
        Write-Host 'Generating a short-lived certificate for Store package construction.' -ForegroundColor Yellow
        Write-Host 'Microsoft Store replaces the package signature after certification; this is not a public distribution credential.' -ForegroundColor Yellow
        $certificateState = New-TemporaryStoreCertificate -Publisher $publisher
        $temporaryCertificate = $true
    }

    if (-not $SkipVerification) {
        $verificationArgs = @(
            '-Task', 'Verify',
            '-SkipSmoke',
            '-ReleaseBuild',
            '-Platform', $Platform
        )
        Assert-Command 'Run native release verification' {
            powershell -NoProfile -ExecutionPolicy Bypass -File $rootExecutor @verificationArgs
        }
    }

    Reset-OutputDirectory -Path $OutputDirectory
    $msbuild = Resolve-MSBuild
    $packageDirectory = $OutputDirectory.TrimEnd('\') + '\'
    $arguments = @(
        $projectPath,
        '/restore',
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        '/p:GenerateAppxPackageOnBuild=true',
        "/p:AppxPackageDir=$packageDirectory",
        '/p:UapAppxPackageBuildMode=StoreUpload',
        '/p:AppxBundle=Always',
        "/p:AppxBundlePlatforms=$Platform",
        '/p:AppxPackageSigningEnabled=true',
        '/p:PackageCertificateThumbprint=',
        "/p:PackageCertificateKeyFile=$($certificateState.path)",
        "/p:PackageCertificatePassword=$($certificateState.password)",
        '/verbosity:minimal',
        '/nologo'
    )

    Assert-Command 'Build Waller Store upload' { & $msbuild @arguments }

    $uploadFiles = @(Get-ChildItem -LiteralPath $OutputDirectory -Recurse -File -Filter '*.msixupload')
    if ($uploadFiles.Count -ne 1) {
        throw "Expected exactly one .msixupload under $OutputDirectory; found $($uploadFiles.Count)."
    }

    $privateFiles = @(Get-ChildItem -LiteralPath $OutputDirectory -Recurse -File | Where-Object {
        $_.Extension -in @('.pfx', '.pvk', '.key')
    })
    if ($privateFiles.Count -gt 0) {
        throw "Private signing material was written into the Store artifact directory: $($privateFiles.FullName -join ', ')"
    }

    $upload = $uploadFiles[0]
    $sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to resolve the source commit.'
    }

    $identityMetadata = Get-Content -LiteralPath (Join-Path $repoRoot 'docs\store\store-identity.json') -Raw | ConvertFrom-Json
    $evidence = [ordered]@{
        schema = 'waller.store-build.v1'
        generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
        sourceCommit = $sourceCommit
        packageName = $packageName
        packageVersion = $packageVersion
        publisher = $publisher
        publisherDisplayName = [string] $identityMetadata.packageIdentity.publisherDisplayName
        storeId = [string] $identityMetadata.store.productId
        platform = $Platform
        configuration = $Configuration
        artifact = [ordered]@{
            name = $upload.Name
            path = $upload.FullName
            bytes = $upload.Length
            sha256 = (Get-FileHash -LiteralPath $upload.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        buildCertificate = [ordered]@{
            thumbprint = $certificateState.thumbprint
            temporary = $certificateState.generated
            note = 'Build/test signature only. Microsoft Store replaces MSIX/AppX signatures after certification.'
        }
    }

    $evidencePath = Join-Path $OutputDirectory 'store-build-manifest.json'
    $evidence | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $evidencePath -Encoding utf8

    Write-Host ''
    Write-Host 'WALLER STORE UPLOAD READY FOR REVIEW' -ForegroundColor Green
    Write-Host "Upload: $($upload.FullName)"
    Write-Host "SHA-256: $($evidence.artifact.sha256)"
    Write-Host "Evidence: $evidencePath"
    Write-Host 'Clean-machine install, launch, Apply, upgrade, uninstall, listing, privacy, and Partner Center checks remain mandatory.' -ForegroundColor Yellow
}
finally {
    if ($temporaryCertificate -and $certificateState) {
        if (Test-Path -LiteralPath $certificateState.path) {
            Remove-Item -LiteralPath $certificateState.path -Force
        }
        if ($certificateState.thumbprint) {
            $storePath = "Cert:\CurrentUser\My\$($certificateState.thumbprint)"
            if (Test-Path -LiteralPath $storePath) {
                Remove-Item -LiteralPath $storePath -Force
            }
        }
    }
}
