param(
    [string]$AppPath = ".\Waller.Native.App\App.xaml.cs",
    [string]$CompositionPath = ".\Waller.Native.App\Platform\WallerAppComposition.cs",
    [string]$MainWindowPath = ".\Waller.Native.App\MainWindow.xaml.cs",
    [string]$MainPagePath = ".\Waller.Native.App\MainPage.xaml.cs",
    [string]$ViewModelPath = ".\Waller.Native.App\ViewModels\MainPageViewModel.cs",
    [string]$PickerPath = ".\Waller.Native.App\Platform\ImageFilePicker.cs"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot

function Read-NativeFile([string]$Path) {
    $fullPath = Join-Path $nativeRoot $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "App composition input not found: $fullPath"
    }

    return Get-Content -LiteralPath $fullPath -Raw
}

$app = Read-NativeFile $AppPath
$composition = Read-NativeFile $CompositionPath
$mainWindow = Read-NativeFile $MainWindowPath
$mainPage = Read-NativeFile $MainPagePath
$viewModel = Read-NativeFile $ViewModelPath
$picker = Read-NativeFile $PickerPath
$errors = @()

function Require-Pattern([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) {
        $script:errors += $Message
    }
}

function Reject-Pattern([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -match $Pattern) {
        $script:errors += $Message
    }
}

Require-Pattern $app 'private\s+Task<WallerAppComposition>\?\s+compositionTask' "App must retain one process composition task."
Require-Pattern $app 'compositionTask\s+\?\?=\s+WallerAppComposition\.CreateAsync\(\)' "App launch must create composition once."
Require-Pattern $app 'composition\s*=\s+await\s+compositionTask' "App launch must observe composition startup."
Require-Pattern $app 'composition\.Window\.Activate\(\)' "App must activate the composed window."
Reject-Pattern $app 'public\s+static[\s\S]*(?:Window|DispatcherQueue|WindowHandle)' "App must not expose static window, dispatcher, or HWND state."

Require-Pattern $composition 'WallerLocalDataStores\.CreateDefault\(\)' "Composition must create local stores once."
Require-Pattern $composition 'new\s+UserSettingsWorkflow\(localData\.Settings\)' "Composition must create the UserSettings workflow from the shared store."
Require-Pattern $composition 'new\s+PresetWorkflow\(localData\.Presets\)' "Composition must create the Preset workflow from the shared store."
Require-Pattern $composition 'new\s+MonitorEditorWorkflow\(\)' "Composition must create one monitor editor workflow."
Require-Pattern $composition 'new\s+ShellWorkspace\(ActiveSession\.FromMonitors\(\[\]\)\)' "Composition must create one shell workspace."
Require-Pattern $composition 'new\s+ApplyWorkflow\(applyService,\s*workspace\)' "Composition must bind Apply to the shared workspace."
Require-Pattern $composition 'new\s+WindowPlacementWorkflow\(userSettings\)' "Composition must create window placement from the shared Settings workflow."
Require-Pattern $composition 'new\s+MainWindow\(windowPlacement\)' "MainWindow must receive the window placement workflow."
Require-Pattern $composition 'WindowNative\.GetWindowHandle\(window\)' "Composition must resolve the concrete window HWND."
Require-Pattern $composition 'new\s+ImageFilePicker\(windowHandle\)' "Picker must receive the concrete HWND."
Require-Pattern $composition 'new\s+WallerAppServices\([\s\S]*new\s+ApplyWorkflow\(applyService,\s*workspace\),[\s\S]*localData,[\s\S]*presets,[\s\S]*userSettings,[\s\S]*workspace\)' "MainPage services must receive the same stores, workflows, and workspace."
Require-Pattern $composition 'new\s+MainPageViewModel\(services\)' "Composition must construct the page ViewModel explicitly."
Require-Pattern $composition 'new\s+MainPage\(viewModel\)' "Composition must construct the page explicitly."
Require-Pattern $composition 'window\.Attach\(page\)' "Composition must attach the constructed page."

if (([regex]::Matches($composition, 'WallerLocalDataStores\.CreateDefault\(\)')).Count -ne 1) {
    $errors += "Composition must create default local stores exactly once."
}

Require-Pattern $mainWindow 'MainWindow\(WindowPlacementWorkflow\s+windowPlacement\)' "MainWindow must require WindowPlacementWorkflow."
Reject-Pattern $mainWindow 'WallerLocalDataStores\.CreateDefault' "MainWindow must not compose its own stores."
Require-Pattern $mainPage 'MainPage\(MainPageViewModel\s+viewModel\)' "MainPage must require its ViewModel."
Reject-Pattern $mainPage 'ViewModel\s*\{\s*get;\s*\}\s*=\s*new' "MainPage must not compose its own ViewModel."
Reject-Pattern $viewModel 'WallerAppServices\.CreateDefault' "MainPageViewModel must not compose default services."

Require-Pattern $picker 'ImageFilePicker\(nint\s+ownerWindowHandle\)' "ImageFilePicker must require a concrete HWND."
Require-Pattern $picker 'ownerWindowHandle\s*==\s*0' "ImageFilePicker must reject an invalid HWND."
Require-Pattern $picker 'Initialize\(picker,\s*ownerWindowHandle\)' "ImageFilePicker must initialize from its injected HWND."
Reject-Pattern $picker 'App\.(?:Window|WindowHandle|DispatcherQueue)' "ImageFilePicker must not consult App globals."

$combined = $app + $composition + $mainWindow + $mainPage + $viewModel + $picker
Reject-Pattern $combined 'ServiceCollection|IServiceProvider|Host\.Create' "App composition must stay explicit and container-free."

if ($errors.Count -gt 0) {
    foreach ($compositionError in $errors) {
        Write-Host "APP COMPOSITION ERROR: $compositionError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "App composition contract passed."
