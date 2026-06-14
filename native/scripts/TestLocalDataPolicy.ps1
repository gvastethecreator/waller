param(
    [string]$AppPath = ".\Waller.Native.App"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedPath = if ([System.IO.Path]::IsPathRooted($AppPath)) {
    $AppPath
}
else {
    Join-Path $nativeRoot $AppPath
}

if (-not (Test-Path -LiteralPath $resolvedPath)) {
    throw "App path not found: $resolvedPath"
}

$appDataPathsPath = Join-Path $resolvedPath "Platform\WallerAppDataPaths.cs"
$storesPath = Join-Path $resolvedPath "Platform\WallerLocalDataStores.cs"
$servicesPath = Join-Path $resolvedPath "Platform\WallerAppServices.cs"

foreach ($path in @($appDataPathsPath, $storesPath, $servicesPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Local data policy input not found: $path"
    }
}

$appDataPathsText = Get-Content -LiteralPath $appDataPathsPath -Raw
$storesText = Get-Content -LiteralPath $storesPath -Raw
$servicesText = Get-Content -LiteralPath $servicesPath -Raw
$errors = @()

if ($appDataPathsText -notmatch 'AppFolderName\s*=\s*"Waller"') {
    $errors += "WallerAppDataPaths.AppFolderName must stay 'Waller'."
}

if ($appDataPathsText -notmatch 'Environment\.GetFolderPath\s*\(\s*Environment\.SpecialFolder\.LocalApplicationData\s*\)') {
    $errors += "Default app-data root must start from Environment.SpecialFolder.LocalApplicationData."
}

if ($appDataPathsText -notmatch 'RenderedRoot\s*\{\s*get;\s*\}\s*=\s*Path\.Combine\s*\(\s*UserVisibleProfileDirectory\s*\(\s*\)\s*,\s*"\.waller"\s*\)' -or
    $appDataPathsText -notmatch 'WindowsIdentity\.GetCurrent\s*\(\s*\)\.User' -or
    $appDataPathsText -notmatch 'ProfileList' -or
    $appDataPathsText -notmatch 'ProfileImagePath' -or
    $appDataPathsText -notmatch 'Environment\.ExpandEnvironmentVariables' -or
    $appDataPathsText -notmatch 'UserProfileFromPackageLocalApplicationData' -or
    $appDataPathsText -notmatch 'LocalCache' -or
    $appDataPathsText -notmatch 'Packages' -or
    $appDataPathsText -notmatch 'Environment\.GetEnvironmentVariable\s*\(\s*"USERPROFILE"\s*\)' -or
    $appDataPathsText -notmatch 'Environment\.SpecialFolder\.UserProfile' -or
    $appDataPathsText -notmatch 'Environment\.SpecialFolder\.ApplicationData') {
    $errors += "Rendered wallpaper root must use a user-profile .waller cache so Windows shell can read rendered PNGs outside package-local AppData virtualization."
}

if ($appDataPathsText -notmatch 'Path\.Combine\s*\(\s*localApplicationDataPath\s*,\s*AppFolderName\s*\)') {
    $errors += "RootFor must compose local app-data root with AppFolderName only."
}

if ($appDataPathsText -match 'Package\.Current|ApplicationData\.Current') {
    $errors += "App-data root must not depend on package identity APIs."
}

if ($storesText -notmatch 'CreateDefault\s*\(\)\s*=>\s*\r?\n\s*Create\s*\(\s*\r?\n\s*WallerAppDataPaths\.Root\s*,\s*\r?\n\s*WallerAppDataPaths\.RenderedRoot\s*\)') {
    $errors += "WallerLocalDataStores.CreateDefault must use WallerAppDataPaths.Root for app-managed JSON and RenderedRoot for rendered wallpapers."
}

foreach ($store in @("PresetStore", "UserSettingsStore")) {
    if ($storesText -notmatch "new\s+$store\s*\(\s*rootDirectory\s*\)") {
        $errors += "WallerLocalDataStores.Create must pass the same rootDirectory to $store."
    }
}

if ($storesText -notmatch "new\s+RenderedWallpaperStore\s*\(\s*renderedRootDirectory\s*\)") {
    $errors += "WallerLocalDataStores.Create must pass renderedRootDirectory to RenderedWallpaperStore."
}

if ($servicesText -notmatch 'WallerLocalDataStores\.CreateDefault\s*\(\s*\)') {
    $errors += "WallerAppServices.CreateDefault must use WallerLocalDataStores.CreateDefault."
}

if ($errors.Count -gt 0) {
    foreach ($error in $errors) {
        Write-Host "LOCAL DATA POLICY ERROR: $error" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Local data policy guard passed."
