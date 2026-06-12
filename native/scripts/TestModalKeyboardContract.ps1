param(
    [string]$MainPageCodeBehindPath = ".\Waller.Native.App\MainPage.xaml.cs",
    [string]$SaveAsModalXamlPath = ".\Waller.Native.App\Controls\SaveAsModal.xaml",
    [string]$SaveAsModalCodeBehindPath = ".\Waller.Native.App\Controls\SaveAsModal.xaml.cs",
    [string]$ManagePresetsModalXamlPath = ".\Waller.Native.App\Controls\ManagePresetsModal.xaml",
    [string]$ManagePresetsModalCodeBehindPath = ".\Waller.Native.App\Controls\ManagePresetsModal.xaml.cs",
    [string]$SettingsModalXamlPath = ".\Waller.Native.App\Controls\SettingsModal.xaml",
    [string]$SettingsModalCodeBehindPath = ".\Waller.Native.App\Controls\SettingsModal.xaml.cs"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot

function Resolve-NativePath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $nativeRoot $Path
}

function Read-RequiredFile {
    param([string]$Path)

    $resolvedPath = Resolve-NativePath $Path
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "Modal keyboard contract input not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw
}

function Assert-ContainsPattern {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -notmatch $Pattern) {
        return $Message
    }

    return $null
}

$mainPage = Read-RequiredFile $MainPageCodeBehindPath
$saveAsXaml = Read-RequiredFile $SaveAsModalXamlPath
$saveAsCode = Read-RequiredFile $SaveAsModalCodeBehindPath
$managePresetsXaml = Read-RequiredFile $ManagePresetsModalXamlPath
$managePresetsCode = Read-RequiredFile $ManagePresetsModalCodeBehindPath
$settingsXaml = Read-RequiredFile $SettingsModalXamlPath
$settingsCode = Read-RequiredFile $SettingsModalCodeBehindPath

$errors = @()

foreach ($check in @(
    @{ Text = $mainPage; Pattern = 'ViewModel\.PropertyChanged\s*\+=\s*OnViewModelPropertyChanged'; Message = "MainPage must use named PropertyChanged handler for modal focus routing." },
    @{ Text = $mainPage; Pattern = 'KeyDown\s*\+=\s*OnKeyDown'; Message = "MainPage must register a named KeyDown handler." },
    @{ Text = $mainPage; Pattern = 'args\.Key\s*!=\s*VirtualKey\.Escape'; Message = "MainPage keyboard handler must check Escape." },
    @{ Text = $mainPage; Pattern = '!ViewModel\.IsAnyModalOpen'; Message = "Escape should only close when a modal is open." },
    @{ Text = $mainPage; Pattern = 'ViewModel\.CloseTopModalCommand\.Execute\s*\(\s*null\s*\)'; Message = "Escape must execute CloseTopModalCommand." },
    @{ Text = $mainPage; Pattern = 'args\.Handled\s*=\s*true'; Message = "Escape modal close must mark the key event handled." },
    @{ Text = $mainPage; Pattern = 'nameof\s*\(\s*ViewModel\.ManagePresetsVisibility\s*\).*ViewModel\.IsManagePresetsOpen[\s\S]*FocusWhenReady\s*\(\s*ManagePresetsModal\.FocusPresetList\s*\)'; Message = "Manage Presets opening must focus the preset list." },
    @{ Text = $mainPage; Pattern = 'nameof\s*\(\s*ViewModel\.SaveAsVisibility\s*\).*ViewModel\.IsSaveAsOpen[\s\S]*FocusWhenReady\s*\(\s*SaveAsModal\.FocusPresetName\s*\)'; Message = "Save As opening must focus the preset name input." },
    @{ Text = $mainPage; Pattern = 'nameof\s*\(\s*ViewModel\.SettingsVisibility\s*\).*ViewModel\.IsSettingsOpen[\s\S]*FocusWhenReady\s*\(\s*SettingsModal\.FocusTheme\s*\)'; Message = "Settings opening must focus the theme picker." },
    @{ Text = $mainPage; Pattern = 'nameof\s*\(\s*ViewModel\.DeleteConfirmationVisibility\s*\).*ViewModel\.IsDeleteConfirmationOpen[\s\S]*FocusWhenReady\s*\(\s*ManagePresetsModal\.FocusConfirmDelete\s*\)'; Message = "Delete confirmation opening must focus the confirm delete action." },
    @{ Text = $mainPage; Pattern = 'DispatcherQueue\.TryEnqueue\s*\(\s*\(\s*\)\s*=>\s*focusAction\s*\(\s*\)\s*\)'; Message = "Modal focus must be deferred through DispatcherQueue." },
    @{ Text = $saveAsXaml; Pattern = 'x:Name="SaveAsPresetNameTextBox"'; Message = "Save As modal must keep a named preset-name input for focus." },
    @{ Text = $saveAsCode; Pattern = 'void\s+FocusPresetName\s*\(\s*\)\s*=>\s*SaveAsPresetNameTextBox\.Focus\s*\(\s*FocusState\.Programmatic\s*\)'; Message = "Save As modal must expose FocusPresetName." },
    @{ Text = $managePresetsXaml; Pattern = 'x:Name="ManagePresetList"'; Message = "Manage Presets modal must keep a named preset list for focus." },
    @{ Text = $managePresetsXaml; Pattern = 'x:Name="ConfirmDeletePresetButton"'; Message = "Manage Presets modal must keep a named confirm-delete action for focus." },
    @{ Text = $managePresetsCode; Pattern = 'void\s+FocusPresetList\s*\(\s*\)\s*=>\s*ManagePresetList\.Focus\s*\(\s*FocusState\.Programmatic\s*\)'; Message = "Manage Presets modal must expose FocusPresetList." },
    @{ Text = $managePresetsCode; Pattern = 'void\s+FocusConfirmDelete\s*\(\s*\)\s*=>\s*ConfirmDeletePresetButton\.Focus\s*\(\s*FocusState\.Programmatic\s*\)'; Message = "Manage Presets modal must expose FocusConfirmDelete." },
    @{ Text = $settingsXaml; Pattern = 'x:Name="SettingsThemeComboBox"'; Message = "Settings modal must keep a named theme picker for focus." },
    @{ Text = $settingsCode; Pattern = 'void\s+FocusTheme\s*\(\s*\)\s*=>\s*SettingsThemeComboBox\.Focus\s*\(\s*FocusState\.Programmatic\s*\)'; Message = "Settings modal must expose FocusTheme." }
)) {
    $failure = Assert-ContainsPattern -Text $check.Text -Pattern $check.Pattern -Message $check.Message
    if ($null -ne $failure) {
        $errors += $failure
    }
}

if ($errors.Count -gt 0) {
    foreach ($error in $errors) {
        Write-Host "MODAL KEYBOARD CONTRACT ERROR: $error" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Modal keyboard contract passed."
