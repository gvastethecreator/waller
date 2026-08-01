param(
    [string]$AppPath = ".\Waller.Native.App\App.xaml.cs",
    [string]$CompositionPath = ".\Waller.Native.App\Platform\WallerAppComposition.cs",
    [string]$MainWindowPath = ".\Waller.Native.App\MainWindow.xaml.cs"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot

function Read-NativeFile([string]$Path) {
    $fullPath = Join-Path $nativeRoot $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Window lifecycle input not found: $fullPath"
    }

    return Get-Content -LiteralPath $fullPath -Raw
}

$app = Read-NativeFile $AppPath
$composition = Read-NativeFile $CompositionPath
$mainWindow = Read-NativeFile $MainWindowPath
$errors = @()

if ($app -notmatch 'compositionTask\s+\?\?=\s+WallerAppComposition\.CreateAsync\(\)' -or
    $app -notmatch 'composition\s*=\s+await\s+compositionTask' -or
    $app -notmatch 'composition\.Window\.Activate\(\)') {
    $errors += "App must await composition and placement restore before activation."
}

if ($composition -notmatch 'await\s+window\.RestorePlacementAsync\(cancellationToken\)' -or
    $composition.IndexOf('await window.RestorePlacementAsync', [System.StringComparison]::Ordinal) -gt
        $composition.IndexOf('return new WallerAppComposition', [System.StringComparison]::Ordinal)) {
    $errors += "Composition must observe placement restore before returning the window."
}

if ($mainWindow -match '_\s*=\s*RestorePlacementAsync|Closed\s*\+=\s*async') {
    $errors += "Window lifecycle tasks must not be discarded or attached to Closed."
}

if ($mainWindow -notmatch 'AppWindow\.Closing\s*\+=\s*OnAppWindowClosing' -or
    $mainWindow -notmatch 'args\.Cancel\s*=\s*true' -or
    $mainWindow -notmatch 'closeTask\s+\?\?=\s+SavePlacementAndDestroyAsync\(\)' -or
    $mainWindow -notmatch 'destroyRequested\s*=\s*true;\s*AppWindow\.Destroy\(\);\s*Application\.Current\.Exit\(\)') {
    $errors += "First close must be cancelled, save once, destroy, and terminate the app explicitly."
}

if ($mainWindow -notmatch 'windowPlacement\.SaveAsync\([\s\S]*new\s+WindowPlacement\(size\.Width,\s*size\.Height,\s*position\.X,\s*position\.Y\)') {
    $errors += "Close save must pass the last complete AppWindow geometry to the workflow."
}

if ($errors.Count -gt 0) {
    foreach ($lifecycleError in $errors) {
        Write-Host "WINDOW LIFECYCLE ERROR: $lifecycleError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Window lifecycle guard passed."
