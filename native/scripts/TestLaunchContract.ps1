param(
    [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest",
    [string]$MainWindowXamlPath = ".\Waller.Native.App\MainWindow.xaml",
    [string]$SmokeLaunchPath = ".\scripts\SmokeLaunch.ps1",
    [string]$BuildAndRunPath = ".\BuildAndRun.ps1"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\PackageManifest.ps1"

$manifestFullPath = Resolve-WallerNativePath $ManifestPath
$mainWindowXamlFullPath = Resolve-WallerNativePath $MainWindowXamlPath
$smokeLaunchFullPath = Resolve-WallerNativePath $SmokeLaunchPath
$buildAndRunFullPath = Resolve-WallerNativePath $BuildAndRunPath

foreach ($path in @($manifestFullPath, $mainWindowXamlFullPath, $smokeLaunchFullPath, $buildAndRunFullPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Launch contract input not found: $path"
    }
}

[xml]$manifest = Read-WallerPackageManifest -ManifestPath $ManifestPath
$mainWindowXaml = Get-Content -LiteralPath $mainWindowXamlFullPath -Raw
$smokeLaunch = Get-Content -LiteralPath $smokeLaunchFullPath -Raw
$buildAndRun = Get-Content -LiteralPath $buildAndRunFullPath -Raw
$errors = @()

function Get-ManifestAttributeByLocalName {
    param(
        [System.Xml.XmlElement]$Node,
        [string]$LocalName
    )

    foreach ($attribute in $Node.Attributes) {
        if ($attribute.LocalName -eq $LocalName) {
            return $attribute.Value
        }
    }

    return $null
}

$application = $manifest.SelectNodes("//*[local-name()='Application']") | Select-Object -First 1
if (-not $application) {
    $errors += "Package manifest must include an Application node."
}
else {
    if ($application.GetAttribute("Id") -ne "App") {
        $errors += "Package Application Id must stay 'App' for stable AUMID suffix."
    }

    if ($application.GetAttribute("Executable") -ne '$targetnametoken$.exe') {
        $errors += "Package Application Executable must stay '$targetnametoken$.exe'."
    }

    if ((Get-ManifestAttributeByLocalName -Node $application -LocalName "RuntimeBehavior") -ne "packagedClassicApp") {
        $errors += "Package Application must run as uap10:RuntimeBehavior='packagedClassicApp'."
    }

    if ((Get-ManifestAttributeByLocalName -Node $application -LocalName "TrustLevel") -ne "mediumIL") {
        $errors += "Package Application must run as uap10:TrustLevel='mediumIL'."
    }
}

if ($mainWindowXaml -notmatch 'Title="Waller"') {
    $errors += "MainWindow title must stay Waller."
}

if ($mainWindowXaml -notmatch '<TitleBar[^>]*\bTitle="Waller"') {
    $errors += "TitleBar title must stay Waller."
}

foreach ($required in @(
    'Join-Path \$nativeRoot "BuildAndRun.ps1"',
    '\$buildArgs = @\(\$ProjectPath, "-Detach"\)',
    'ConvertFrom-Json',
    'Launch JSON did not include ProcessId',
    'ProcessName -ne "Waller.Native.App"',
    'MainWindowTitle -ne "Waller"',
    'Responding',
    'Stop-LaunchedApp -ProcessId \$appProcessId')) {
    if ($smokeLaunch -notmatch $required) {
        $errors += "SmokeLaunch.ps1 missing launch contract pattern: $required"
    }
}

if ($smokeLaunch -match 'Waller\.Native\.App\.exe') {
    $errors += "SmokeLaunch.ps1 must not launch the app exe directly."
}

foreach ($required in @(
    'Find-WinApp',
    'winappPath run \$outputDir --detach --json',
    'winappPath run \$outputDir --debug-output')) {
    if ($buildAndRun -notmatch $required) {
        $errors += "BuildAndRun.ps1 missing winapp launch pattern: $required"
    }
}

if ($errors.Count -gt 0) {
    foreach ($error in $errors) {
        Write-Host "LAUNCH CONTRACT ERROR: $error" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Launch contract guard passed."
