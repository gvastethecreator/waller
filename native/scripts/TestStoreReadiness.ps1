#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter()]
    [switch] $RequireReservedIdentity,

    [Parameter()]
    [string] $EvidencePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$nativeRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $nativeRoot
$manifestPath = Join-Path $nativeRoot 'Waller.Native.App\Package.appxmanifest'
$packageRoot = Join-Path $nativeRoot 'Waller.Native.App'
$identityPath = Join-Path $repoRoot 'docs\store\store-identity.json'
$privacyPath = Join-Path $repoRoot 'PRIVACY.md'
$runbookPath = Join-Path $repoRoot 'docs\store\README.md'
$listingPath = Join-Path $repoRoot 'docs\store\LISTING.md'
$certificationNotesPath = Join-Path $repoRoot 'docs\store\CERTIFICATION-NOTES.md'
$evidenceTemplatePath = Join-Path $repoRoot 'docs\store\RELEASE-EVIDENCE-TEMPLATE.md'
$setIdentityPath = Join-Path $PSScriptRoot 'SetStoreIdentity.ps1'

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$checks = [System.Collections.Generic.List[object]]::new()

function Get-RepoRelativePath {
    param([Parameter(Mandatory)] [string] $Path)

    $root = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($root.Length)
    }
    return $fullPath
}

function Add-Check {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [bool] $Passed,
        [Parameter(Mandatory)] [string] $Details
    )

    $checks.Add([ordered]@{ name = $Name; passed = $Passed; details = $Details })
    if (-not $Passed) {
        $errors.Add("$Name`: $Details")
    }
}

function Add-Warning {
    param([Parameter(Mandatory)] [string] $Message)
    $warnings.Add($Message)
}

function Test-ExactText {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [AllowNull()] [string] $Actual,
        [AllowNull()] [string] $Expected
    )

    Add-Check `
        -Name $Name `
        -Passed ([string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) `
        -Details "expected '$Expected', actual '$Actual'"

    if ($null -ne $Actual) {
        Add-Check `
            -Name "$Name has no surrounding whitespace" `
            -Passed ([string]::Equals($Actual, $Actual.Trim(), [StringComparison]::Ordinal) -and $Actual.IndexOf([char] 0x00A0) -lt 0) `
            -Details 'value must not contain leading, trailing, or non-breaking whitespace'
    }
}

foreach ($requiredFile in @(
    $manifestPath,
    $identityPath,
    $privacyPath,
    $runbookPath,
    $listingPath,
    $certificationNotesPath,
    $evidenceTemplatePath,
    $setIdentityPath
)) {
    Add-Check `
        -Name "Required file: $(Get-RepoRelativePath -Path $requiredFile)" `
        -Passed (Test-Path -LiteralPath $requiredFile -PathType Leaf) `
        -Details 'file must exist'
}

if ($errors.Count -gt 0) {
    throw "Store readiness prerequisites are missing:`n - $($errors -join "`n - ")"
}

$storeIdentity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
[xml] $manifest = Get-Content -LiteralPath $manifestPath -Raw
$manifestText = $manifest.OuterXml

$ns = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
$ns.AddNamespace('f', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$ns.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')
$ns.AddNamespace('uap10', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/10')
$ns.AddNamespace('rescap', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities')

$identityNode = $manifest.SelectSingleNode('/f:Package/f:Identity', $ns)
$publisherDisplayNameNode = $manifest.SelectSingleNode('/f:Package/f:Properties/f:PublisherDisplayName', $ns)
$applicationNode = $manifest.SelectSingleNode('/f:Package/f:Applications/f:Application[@Id="App"]', $ns)
$fullTrustNode = $manifest.SelectSingleNode('/f:Package/f:Capabilities/rescap:Capability[@Name="runFullTrust"]', $ns)

Add-Check -Name 'Manifest Identity node' -Passed ($null -ne $identityNode) -Details 'Package.appxmanifest must contain Package/Identity'
Add-Check -Name 'Stable application ID' -Passed ($null -ne $applicationNode) -Details 'Application Id must remain App so the AUMID suffix remains !App'
Add-Check -Name 'runFullTrust declaration' -Passed ($null -ne $fullTrustNode) -Details 'desktop wallpaper operations require the declared restricted capability and Partner Center explanation'

if ($applicationNode) {
    Add-Check -Name 'Packaged classic runtime' -Passed ($applicationNode.GetAttribute('RuntimeBehavior', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/10') -eq 'packagedClassicApp') -Details 'uap10:RuntimeBehavior must be packagedClassicApp'
    Add-Check -Name 'Medium integrity trust level' -Passed ($applicationNode.GetAttribute('TrustLevel', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/10') -eq 'mediumIL') -Details 'uap10:TrustLevel must remain mediumIL for normal non-elevated use'
}

$targetFamilies = @($manifest.SelectNodes('/f:Package/f:Dependencies/f:TargetDeviceFamily', $ns))
$targetNames = @($targetFamilies | ForEach-Object { $_.GetAttribute('Name') })
Add-Check -Name 'One target device family' -Passed ($targetFamilies.Count -eq 1) -Details "expected Windows.Desktop only; found $($targetFamilies.Count)"
Add-Check -Name 'Desktop-only package' -Passed ($targetNames.Count -eq 1 -and $targetNames[0] -eq 'Windows.Desktop') -Details "target families: $($targetNames -join ', ')"
if ($targetFamilies.Count -gt 0) {
    Add-Check -Name 'Minimum Windows version' -Passed ($targetFamilies[0].GetAttribute('MinVersion') -eq '10.0.17763.0') -Details "expected 10.0.17763.0; actual $($targetFamilies[0].GetAttribute('MinVersion'))"
}

if ($identityNode) {
    $versionText = $identityNode.GetAttribute('Version')
    $parts = $versionText.Split('.')
    $validVersion = $versionText -match '^\d+\.\d+\.\d+\.\d+$' -and $parts.Count -eq 4
    if ($validVersion) {
        foreach ($part in $parts) {
            $number = 0
            if (-not [int]::TryParse($part, [ref] $number) -or $number -lt 0 -or $number -gt 65535) {
                $validVersion = $false
                break
            }
        }
    }
    Add-Check -Name 'MSIX version' -Passed $validVersion -Details "'$versionText' must contain four numeric components from 0 through 65535"
}

$status = [string] $storeIdentity.reservationStatus
Add-Check -Name 'Known reservation status' -Passed ($status -in @('pending', 'reserved')) -Details "reservationStatus must be pending or reserved; actual '$status'"

if ($status -eq 'reserved') {
    foreach ($field in @(
        @{ Name = 'Reserved identity name'; Actual = [string] $storeIdentity.packageIdentity.name },
        @{ Name = 'Reserved publisher'; Actual = [string] $storeIdentity.packageIdentity.publisher },
        @{ Name = 'Reserved publisher display name'; Actual = [string] $storeIdentity.packageIdentity.publisherDisplayName },
        @{ Name = 'Reserved PFN'; Actual = [string] $storeIdentity.packageIdentity.packageFamilyName },
        @{ Name = 'Reserved Package SID'; Actual = [string] $storeIdentity.packageIdentity.packageSid },
        @{ Name = 'Reserved Store ID'; Actual = [string] $storeIdentity.store.productId }
    )) {
        Add-Check -Name $field.Name -Passed (-not [string]::IsNullOrWhiteSpace($field.Actual)) -Details 'reserved identity values must be populated'
    }

    if ($identityNode) {
        Test-ExactText -Name 'Manifest Store identity name' -Actual $identityNode.GetAttribute('Name') -Expected $storeIdentity.packageIdentity.name
        Test-ExactText -Name 'Manifest Store publisher' -Actual $identityNode.GetAttribute('Publisher') -Expected $storeIdentity.packageIdentity.publisher
    }
    Test-ExactText -Name 'Manifest Store publisher display name' -Actual $(if ($publisherDisplayNameNode) { $publisherDisplayNameNode.InnerText } else { $null }) -Expected $storeIdentity.packageIdentity.publisherDisplayName

    Add-Check -Name 'PFN remains metadata-only' -Passed ($manifestText.IndexOf([string] $storeIdentity.packageIdentity.packageFamilyName, [StringComparison]::Ordinal) -lt 0) -Details 'PFN must not be written into Package.appxmanifest'
    Add-Check -Name 'Package SID remains metadata-only' -Passed ($manifestText.IndexOf([string] $storeIdentity.packageIdentity.packageSid, [StringComparison]::Ordinal) -lt 0) -Details 'Package SID must not be written into Package.appxmanifest'

    $privacyText = Get-Content -LiteralPath $privacyPath -Raw
    Add-Check -Name 'Privacy publisher finalized' -Passed ($privacyText.IndexOf('To be completed with the verified Microsoft Store publisher name', [StringComparison]::OrdinalIgnoreCase) -lt 0 -and $privacyText.IndexOf('debe completarse con el nombre verificado', [StringComparison]::OrdinalIgnoreCase) -lt 0) -Details 'replace the pending publisher text in PRIVACY.md after reservation'
}
else {
    if ($identityNode) {
        Test-ExactText -Name 'Development identity name' -Actual $identityNode.GetAttribute('Name') -Expected $storeIdentity.developmentIdentity.name
        Test-ExactText -Name 'Development publisher' -Actual $identityNode.GetAttribute('Publisher') -Expected $storeIdentity.developmentIdentity.publisher
    }
    Test-ExactText -Name 'Development publisher display name' -Actual $(if ($publisherDisplayNameNode) { $publisherDisplayNameNode.InnerText } else { $null }) -Expected $storeIdentity.developmentIdentity.publisherDisplayName
    Add-Warning 'Partner Center identity is still pending. Structural validation can pass, but a Store upload must not be submitted or built as final.'
    if ($RequireReservedIdentity) {
        Add-Check -Name 'Reserved Partner Center identity required' -Passed $false -Details 'run SetStoreIdentity.ps1 with the exact Product identity values before building a submission'
    }
}

$requiredAssets = @(
    'Assets\StoreLogo.png',
    'Assets\Square150x150Logo.scale-200.png',
    'Assets\Square44x44Logo.scale-200.png',
    'Assets\Wide310x150Logo.scale-200.png',
    'Assets\SplashScreen.scale-200.png',
    'Assets\AppIcon.ico'
)
foreach ($asset in $requiredAssets) {
    Add-Check -Name "Package asset $asset" -Passed (Test-Path -LiteralPath (Join-Path $packageRoot $asset) -PathType Leaf) -Details 'required package asset must exist'
}

$result = [ordered]@{
    schema = 'waller.store-readiness.v1'
    generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
    repository = 'gvastethecreator/waller'
    reservationStatus = $status
    manifest = Get-RepoRelativePath -Path $manifestPath
    passed = $errors.Count -eq 0
    warnings = $warnings
    checks = $checks
}

if ($EvidencePath) {
    $resolvedEvidencePath = if ([System.IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $repoRoot $EvidencePath }
    $directory = Split-Path -Parent $resolvedEvidencePath
    if ($directory) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }
    $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedEvidencePath -Encoding utf8
    Write-Host "Evidence: $resolvedEvidencePath" -ForegroundColor DarkGray
}

foreach ($warning in $warnings) {
    Write-Host "WARNING: $warning" -ForegroundColor Yellow
}

if ($errors.Count -gt 0) {
    Write-Host ''
    Write-Host 'STORE READINESS FAILED' -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host " - $errorMessage" -ForegroundColor Red
    }
    exit 1
}

Write-Host ''
Write-Host "STORE READINESS PASSED ($($checks.Count) checks)" -ForegroundColor Green
Write-Host "Reservation status: $status"
