param(
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [string]$MainWindowXamlPath = ".\Waller.Native.App\MainWindow.xaml",
    [string]$MainWindowCodePath = ".\Waller.Native.App\MainWindow.xaml.cs"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"

$manifestFullPath = Resolve-WallerNativePath $ManifestPath
$projectFullPath = Resolve-WallerNativePath $ProjectPath
$mainWindowXamlFullPath = Resolve-WallerNativePath $MainWindowXamlPath
$mainWindowCodeFullPath = Resolve-WallerNativePath $MainWindowCodePath

foreach ($path in @($manifestFullPath, $projectFullPath, $mainWindowXamlFullPath, $mainWindowCodeFullPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Package asset lint input not found: $path"
    }
}

$appProjectRoot = Split-Path -Parent $projectFullPath
[xml]$manifest = Read-WallerPackageManifest -ManifestPath $ManifestPath
[xml]$project = Get-Content -LiteralPath $projectFullPath -Raw
$mainWindowXaml = Get-Content -LiteralPath $mainWindowXamlFullPath -Raw
$mainWindowCode = Get-Content -LiteralPath $mainWindowCodeFullPath -Raw

$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error {
    param([string]$Message)
    $errors.Add($Message)
}

function Test-AssetReference {
    param([string]$Reference)

    if ([string]::IsNullOrWhiteSpace($Reference)) {
        return
    }

    $normalized = $Reference.Replace("/", "\")
    $exactPath = Join-Path $appProjectRoot $normalized
    if (Test-Path -LiteralPath $exactPath) {
        return
    }

    $directory = Split-Path -Parent $exactPath
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($exactPath)
    $extension = [System.IO.Path]::GetExtension($exactPath)
    $scaleQualifiedMatch = Get-ChildItem -LiteralPath $directory -Filter "$fileName.*$extension" -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($scaleQualifiedMatch) {
        return
    }

    Add-Error "Asset reference '$Reference' does not resolve to an exact or scale-qualified file under $appProjectRoot."
}

if ($manifest.Package.Properties.DisplayName -ne "Waller") {
    Add-Error "Package DisplayName must be Waller."
}

if ([string]::IsNullOrWhiteSpace($manifest.Package.Identity.Name)) {
    Add-Error "Package Identity Name must not be empty."
}

if ($manifest.Package.Identity.Name -match "Waller\.Native\.App|AppDisplayName|AppPublisher") {
    Add-Error "Package Identity Name must not use template text."
}

if ($manifest.Package.Properties.PublisherDisplayName -ne "Waller") {
    Add-Error "Package PublisherDisplayName must be Waller."
}

if ($manifest.Package.Identity.Publisher -ne "CN=Waller") {
    Add-Error "Package Publisher must be CN=Waller."
}

foreach ($namespace in @("uap10", "rescap")) {
    if ($manifest.Package.IgnorableNamespaces -notmatch "(^|\s)$namespace(\s|$)") {
        Add-Error "Package IgnorableNamespaces must include $namespace."
    }
}

if (-not (Test-WallerMsixVersion -Value $manifest.Package.Identity.Version)) {
    Add-Error "Package Identity Version must use four numeric parts between 0 and 65535."
}

$visualElements = $manifest.SelectNodes("//*[local-name()='VisualElements']") | Select-Object -First 1
if (-not $visualElements) {
    Add-Error "Package manifest must include uap:VisualElements."
}
else {
    if ($visualElements.GetAttribute("DisplayName") -ne "Waller") {
        Add-Error "VisualElements DisplayName must be Waller."
    }

    if ($visualElements.GetAttribute("Description") -match "Waller\.Native\.App|AppDisplayName") {
        Add-Error "VisualElements Description must not use template text."
    }
}

$manifestAssetReferences = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($node in $manifest.SelectNodes("//*")) {
    if ($node.LocalName -eq "Logo" -and -not [string]::IsNullOrWhiteSpace($node.InnerText)) {
        [void]$manifestAssetReferences.Add($node.InnerText)
    }

    foreach ($attribute in @("Square150x150Logo", "Square44x44Logo", "Wide310x150Logo", "Image")) {
        $value = $node.GetAttribute($attribute)
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            [void]$manifestAssetReferences.Add($value)
        }
    }
}

foreach ($reference in $manifestAssetReferences) {
    Test-AssetReference $reference
}

$projectContentReferences = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($node in $project.GetElementsByTagName("Content")) {
    $include = $node.GetAttribute("Include")
    if (-not [string]::IsNullOrWhiteSpace($include)) {
        [void]$projectContentReferences.Add($include.Replace("/", "\"))
    }
}

foreach ($reference in $manifestAssetReferences) {
    $normalized = $reference.Replace("/", "\")
    $exactIncluded = $projectContentReferences.Contains($normalized)
    $referencePath = Join-Path $appProjectRoot $normalized
    $directory = Split-Path -Parent $referencePath
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($referencePath)
    $extension = [System.IO.Path]::GetExtension($referencePath)
    $scaleIncluded = $false

    foreach ($include in $projectContentReferences) {
        $includePath = Join-Path $appProjectRoot $include
        if ((Split-Path -Parent $includePath) -eq $directory -and
            [System.IO.Path]::GetFileNameWithoutExtension($includePath).StartsWith("$fileName.", [StringComparison]::OrdinalIgnoreCase) -and
            [System.IO.Path]::GetExtension($includePath) -eq $extension) {
            $scaleIncluded = $true
            break
        }
    }

    if (-not $exactIncluded -and -not $scaleIncluded) {
        Add-Error "Asset reference '$reference' is not included as Content in $ProjectPath."
    }
}

$appIconReferences = @()
$appIconReferences += [regex]::Matches($mainWindowXaml, "Assets[/\\]AppIcon\.ico") | ForEach-Object { $_.Value }
$appIconReferences += [regex]::Matches($mainWindowCode, "Assets[/\\]AppIcon\.ico") | ForEach-Object { $_.Value }
if ($appIconReferences.Count -lt 2) {
    Add-Error "MainWindow must use Assets\AppIcon.ico in both XAML TitleBar and AppWindow.SetIcon."
}

$runFullTrustCapability = $manifest.SelectNodes("//*[local-name()='Capability']") |
    Where-Object { $_.GetAttribute("Name") -eq "runFullTrust" } |
    Select-Object -First 1
if (-not $runFullTrustCapability) {
    Add-Error "Package manifest must declare rescap:Capability Name='runFullTrust'."
}

Test-AssetReference "Assets\AppIcon.ico"
if (-not $projectContentReferences.Contains("Assets\AppIcon.ico")) {
    Add-Error "Assets\AppIcon.ico must be included as Content in $ProjectPath."
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Host "PACKAGE ASSET LINT ERROR: $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Package asset/identity lint passed."
