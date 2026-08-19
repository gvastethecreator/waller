#Requires -Version 5.1

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [string] $Name,

    [Parameter(Mandatory)]
    [string] $Publisher,

    [Parameter(Mandatory)]
    [string] $PublisherDisplayName,

    [Parameter(Mandatory)]
    [string] $PackageFamilyName,

    [Parameter(Mandatory)]
    [string] $PackageSid,

    [Parameter(Mandatory)]
    [string] $StoreId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$nativeRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $nativeRoot
$manifestPath = Join-Path $nativeRoot 'Waller.Native.App\Package.appxmanifest'
$identityPath = Join-Path $repoRoot 'docs\store\store-identity.json'
$privacyPath = Join-Path $repoRoot 'PRIVACY.md'
$readinessScript = Join-Path $PSScriptRoot 'TestStoreReadiness.ps1'

function Assert-ExactInput {
    param(
        [Parameter(Mandatory)] [string] $Field,
        [AllowEmptyString()] [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Field is required."
    }

    if (-not [string]::Equals($Value, $Value.Trim(), [StringComparison]::Ordinal)) {
        throw "$Field contains leading, trailing, or non-breaking whitespace. Copy the exact Partner Center value without surrounding spaces."
    }

    if ($Value.IndexOf([char] 0x00A0) -ge 0) {
        throw "$Field contains a non-breaking space. Remove it before continuing."
    }
}

foreach ($entry in @(
    @{ Field = 'Package/Identity/Name'; Value = $Name },
    @{ Field = 'Package/Identity/Publisher'; Value = $Publisher },
    @{ Field = 'PublisherDisplayName'; Value = $PublisherDisplayName },
    @{ Field = 'Package Family Name'; Value = $PackageFamilyName },
    @{ Field = 'Package SID'; Value = $PackageSid },
    @{ Field = 'Store ID'; Value = $StoreId }
)) {
    Assert-ExactInput -Field $entry.Field -Value $entry.Value
}

if (-not $Publisher.StartsWith('CN=', [StringComparison]::Ordinal)) {
    throw "Publisher must be the exact Partner Center distinguished name and normally starts with 'CN=': $Publisher"
}
if ($PackageSid -notmatch '^S-1-15-2-(?:\d+-){6}\d+$') {
    throw "Package SID does not match the expected app-container SID form: $PackageSid"
}
if ($StoreId -notmatch '^[A-Z0-9]{12}$') {
    throw "Store ID must contain 12 uppercase letters or digits: $StoreId"
}

[xml] $manifest = Get-Content -LiteralPath $manifestPath -Raw
$namespaceManager = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
$namespaceManager.AddNamespace('f', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$identityNode = $manifest.SelectSingleNode('/f:Package/f:Identity', $namespaceManager)
$publisherDisplayNameNode = $manifest.SelectSingleNode('/f:Package/f:Properties/f:PublisherDisplayName', $namespaceManager)
if ($null -eq $identityNode -or $null -eq $publisherDisplayNameNode) {
    throw 'Package.appxmanifest is missing the expected Identity or PublisherDisplayName node.'
}

$storeIdentity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
$privacyText = Get-Content -LiteralPath $privacyPath -Raw

$identityNode.SetAttribute('Name', $Name)
$identityNode.SetAttribute('Publisher', $Publisher)
$publisherDisplayNameNode.InnerText = $PublisherDisplayName

$storeIdentity.reservationStatus = 'reserved'
$storeIdentity.packageIdentity.name = $Name
$storeIdentity.packageIdentity.publisher = $Publisher
$storeIdentity.packageIdentity.publisherDisplayName = $PublisherDisplayName
$storeIdentity.packageIdentity.packageFamilyName = $PackageFamilyName
$storeIdentity.packageIdentity.packageSid = $PackageSid
$storeIdentity.store.productId = $StoreId
$storeIdentity.store.deepLink = $null
$storeIdentity.store.webStoreUrl = $null

$privacyText = $privacyText.Replace(
    '**Publisher:** To be completed with the verified Microsoft Store publisher name before submission',
    "**Publisher:** $PublisherDisplayName"
)
$privacyText = $privacyText.Replace(
    '**Publicador:** debe completarse con el nombre verificado de Microsoft Store antes de la submission',
    "**Publicador:** $PublisherDisplayName"
)

if (-not $PSCmdlet.ShouldProcess('Waller package manifest, Store identity metadata, and privacy publisher', 'Apply Partner Center identity')) {
    return
}

$manifestSettings = [System.Xml.XmlWriterSettings]::new()
$manifestSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
$manifestSettings.Indent = $true
$manifestSettings.NewLineChars = "`n"
$manifestSettings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

$manifestTemp = "$manifestPath.tmp"
$identityTemp = "$identityPath.tmp"
$privacyTemp = "$privacyPath.tmp"
try {
    $writer = [System.Xml.XmlWriter]::Create($manifestTemp, $manifestSettings)
    try {
        $manifest.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    $storeIdentity | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $identityTemp -Encoding utf8
    [System.IO.File]::WriteAllText($privacyTemp, $privacyText, [System.Text.UTF8Encoding]::new($false))

    Move-Item -LiteralPath $manifestTemp -Destination $manifestPath -Force
    Move-Item -LiteralPath $identityTemp -Destination $identityPath -Force
    Move-Item -LiteralPath $privacyTemp -Destination $privacyPath -Force
}
finally {
    foreach ($tempPath in @($manifestTemp, $identityTemp, $privacyTemp)) {
        if (Test-Path -LiteralPath $tempPath) {
            Remove-Item -LiteralPath $tempPath -Force
        }
    }
}

Write-Host 'Waller Store identity applied.' -ForegroundColor Green
Write-Host "Name: $Name"
Write-Host "Publisher: $Publisher"
Write-Host "Publisher display name: $PublisherDisplayName"
Write-Host "PFN (verification only): $PackageFamilyName"
Write-Host "Package SID (verification only): $PackageSid"
Write-Host "Store ID: $StoreId"

if (Test-Path -LiteralPath $readinessScript) {
    & $readinessScript -RequireReservedIdentity
    if ($LASTEXITCODE -ne 0) {
        throw 'Store identity was written, but the readiness gate failed.'
    }
}
