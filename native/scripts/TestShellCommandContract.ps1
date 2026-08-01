param(
    [string]$ShellHeaderXamlPath = ".\Waller.Native.App\Controls\ShellHeader.xaml"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$shellHeaderFullPath = if ([System.IO.Path]::IsPathRooted($ShellHeaderXamlPath)) {
    $ShellHeaderXamlPath
}
else {
    Join-Path $nativeRoot $ShellHeaderXamlPath
}

if (-not (Test-Path -LiteralPath $shellHeaderFullPath)) {
    throw "Shell command contract input not found: $shellHeaderFullPath"
}

[xml]$xaml = Get-Content -LiteralPath $shellHeaderFullPath -Raw
$errors = @()

function Get-ControlByAutomationId {
    param([string]$AutomationId)

    return $xaml.SelectSingleNode("//*[@AutomationProperties.AutomationId='$AutomationId']")
}

function Get-Accelerator {
    param($Button)

    return $Button.SelectSingleNode(".//*[local-name()='KeyboardAccelerator']")
}

function Assert-RequiredBinding {
    param(
        $Node,
        [string]$AttributeName,
        [string]$ExpectedFragment,
        [string]$Message
    )

    $value = $Node.GetAttribute($AttributeName)
    if ($value -notlike "*$ExpectedFragment*") {
        $script:errors += $Message
    }
}

$presetComboBox = Get-ControlByAutomationId "PresetComboBox"
if ($null -eq $presetComboBox) {
    $errors += "ShellHeader must keep PresetComboBox in the top shell."
}
else {
    Assert-RequiredBinding $presetComboBox "IsEnabled" "CanUseShellCommands" "PresetComboBox must stay disabled while shell commands are blocked."
    Assert-RequiredBinding $presetComboBox "ItemsSource" "Presets.Items" "PresetComboBox must use the focused Presets catalog."
    Assert-RequiredBinding $presetComboBox "SelectedItem" "Presets.SelectedPreset" "PresetComboBox must stay bound to the focused selection."
}

$expectedCommands = @(
    @{
        AutomationId = "SaveButton"
        Command = "Presets.SaveCommand"
        IsEnabled = "CanUseShellCommands"
        Key = "S"
        Modifiers = "Control"
    },
    @{
        AutomationId = "SaveAsButton"
        Command = "Presets.SaveAsCommand"
        IsEnabled = "CanUseShellCommands"
        Key = "S"
        Modifiers = "Control,Shift"
    },
    @{
        AutomationId = "ManagePresetsButton"
        Command = "Presets.ManagePresetsCommand"
        IsEnabled = "CanUseShellCommands"
        Key = "M"
        Modifiers = "Control"
    },
    @{
        AutomationId = "RefreshButton"
        Command = "RefreshCommand"
        IsEnabled = "CanStartApply"
        Key = "R"
        Modifiers = "Control"
    },
    @{
        AutomationId = "SettingsButton"
        Command = "OpenSettingsCommand"
        IsEnabled = "CanUseShellCommands"
        Key = "I"
        Modifiers = "Control"
    },
    @{
        AutomationId = "ApplyAllButton"
        Command = "Apply.ApplyAllCommand"
        IsEnabled = "Apply.CanStartApply"
        Key = "Enter"
        Modifiers = "Control"
    }
)

foreach ($expected in $expectedCommands) {
    $button = Get-ControlByAutomationId $expected.AutomationId
    if ($null -eq $button) {
        $errors += "ShellHeader missing $($expected.AutomationId)."
        continue
    }

    Assert-RequiredBinding $button "Command" $expected.Command "$($expected.AutomationId) must stay bound to $($expected.Command)."
    Assert-RequiredBinding $button "IsEnabled" $expected.IsEnabled "$($expected.AutomationId) must stay gated by $($expected.IsEnabled)."

    $accelerator = Get-Accelerator $button
    if ($null -eq $accelerator) {
        $errors += "$($expected.AutomationId) missing KeyboardAccelerator."
        continue
    }

    if ($accelerator.GetAttribute("Key") -ne $expected.Key) {
        $errors += "$($expected.AutomationId) accelerator key must stay $($expected.Key)."
    }

    if ($accelerator.GetAttribute("Modifiers") -ne $expected.Modifiers) {
        $errors += "$($expected.AutomationId) accelerator modifiers must stay $($expected.Modifiers)."
    }
}

$scrollViewer = $xaml.SelectSingleNode("//*[local-name()='ScrollViewer' and @HorizontalScrollBarVisibility='Auto' and @HorizontalScrollMode='Auto']")
if ($null -eq $scrollViewer) {
    $errors += "Shell command row must stay inside a horizontal ScrollViewer so narrow windows do not force header overflow."
}

if ($errors.Count -gt 0) {
    foreach ($contractError in $errors) {
        Write-Host "SHELL COMMAND CONTRACT ERROR: $contractError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Shell command contract passed."
