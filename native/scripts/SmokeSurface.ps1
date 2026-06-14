param(
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [int]$LaunchTimeoutSeconds = 10,
    [switch]$DisableNuGetAudit,
    [switch]$SettingsRoundTrip
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $nativeRoot "BuildAndRun.ps1"
$appProcessId = $null
$localDataRoot = $null
$settingsPath = $null
$settingsBackupPath = Join-Path ([System.IO.Path]::GetTempPath()) "waller-settings-smoke-$([guid]::NewGuid().ToString("N")).json"
$hadSettingsFile = $false

function Assert-LastExitCode {
    param([string]$Step)

    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Stop-LaunchedApp {
    param([int]$ProcessId)

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        return
    }

    $null = $process.CloseMainWindow()
    Start-Sleep -Seconds 1
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $ProcessId -Force
    }
}

function Find-WallerElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)

    return $Root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        $condition)
}

function Wait-WallerElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId,
        [int]$TimeoutSeconds = 5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $element = Find-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
        if ($element) {
            return $element
        }

        Start-Sleep -Milliseconds 150
    }

    throw "UI Automation element not found: $AutomationId."
}

function Assert-WallerElementsPresent {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string[]]$AutomationIds,
        [string]$ScopeName
    )

    $missing = @()
    foreach ($automationId in $AutomationIds) {
        if (-not (Find-WallerElementByAutomationId -Root $Root -AutomationId $automationId)) {
            $missing += $automationId
        }
    }

    [pscustomobject]@{
        Scope = $ScopeName
        RequiredControls = $AutomationIds.Count
        MissingControls = $missing.Count
        Missing = $missing -join ", "
    } | Format-List | Out-String | Write-Host

    if ($missing.Count -gt 0) {
        throw "Missing UI Automation controls in ${ScopeName}: $($missing -join ', ')"
    }
}

function Invoke-WallerElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $element = Wait-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
    if (-not $element.Current.IsEnabled) {
        throw "UI Automation element is disabled: $AutomationId."
    }

    $pattern = $null
    if (-not $element.TryGetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern,
        [ref]$pattern)) {
        throw "UI Automation element does not support InvokePattern: $AutomationId."
    }

    $pattern.Invoke()
}

function Set-WallerSettingsPathFromLaunch {
    param([string]$Aumid)

    $localApplicationData = [Environment]::GetEnvironmentVariable("LOCALAPPDATA")
    if ([string]::IsNullOrWhiteSpace($localApplicationData)) {
        $localApplicationData = [Environment]::GetFolderPath("LocalApplicationData")
    }

    if ($Aumid -match "^(.+)!App$") {
        $script:localDataRoot = Join-Path $localApplicationData "Packages\$($Matches[1])\LocalCache\Local\Waller"
    }
    else {
        $script:localDataRoot = Join-Path $localApplicationData "Waller"
    }

    $script:settingsPath = Join-Path $script:localDataRoot "settings.json"
}

function Backup-WallerSettings {
    if (-not $SettingsRoundTrip) {
        return
    }

    if (Test-Path -LiteralPath $settingsPath) {
        Copy-Item -LiteralPath $settingsPath -Destination $settingsBackupPath -Force
        $script:hadSettingsFile = $true
        return
    }

    $script:hadSettingsFile = $false
}

function Restore-WallerSettings {
    if (-not $SettingsRoundTrip) {
        return
    }

    if ($script:hadSettingsFile) {
        New-Item -ItemType Directory -Path $localDataRoot -Force | Out-Null
        Copy-Item -LiteralPath $settingsBackupPath -Destination $settingsPath -Force
    }
    elseif (Test-Path -LiteralPath $settingsPath) {
        Remove-Item -LiteralPath $settingsPath -Force
    }

    if (Test-Path -LiteralPath $settingsBackupPath) {
        Remove-Item -LiteralPath $settingsBackupPath -Force
    }
}

function Select-WallerComboBoxItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId,
        [string[]]$Names,
        [switch]$UseLastItemKeyboardFallback,
        [switch]$SkipSelectionVerification
    )

    $comboBox = Wait-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
    if (-not $comboBox.Current.IsEnabled) {
        throw "UI Automation element is disabled: $AutomationId."
    }

    $expandPattern = $null
    if (-not $comboBox.TryGetCurrentPattern(
        [System.Windows.Automation.ExpandCollapsePattern]::Pattern,
        [ref]$expandPattern)) {
        throw "UI Automation element does not support ExpandCollapsePattern: $AutomationId."
    }

    $expandPattern.Expand()
    Start-Sleep -Milliseconds 250

    $processId = $Root.Current.ProcessId
    foreach ($name in $Names) {
        $condition = [System.Windows.Automation.AndCondition]::new(
            [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::NameProperty,
                $name),
            [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::ListItem),
            [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
                $processId))

        $item = $comboBox.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants,
            $condition)
        if (-not $item) {
            $item = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                [System.Windows.Automation.TreeScope]::Descendants,
                $condition)
        }
        if (-not $item) {
            continue
        }

        $item.SetFocus()
        Start-Sleep -Milliseconds 100

        $selectionPattern = $null
        if ($item.TryGetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern,
            [ref]$selectionPattern)) {
            $selectionPattern.Select()
        }
        else {
            $invokePattern = $null
            if (-not $item.TryGetCurrentPattern(
                [System.Windows.Automation.InvokePattern]::Pattern,
                [ref]$invokePattern)) {
                throw "UI Automation combo item does not support SelectionItemPattern or InvokePattern: $name."
            }

            $invokePattern.Invoke()
        }

        Start-Sleep -Milliseconds 700
        if ($SkipSelectionVerification -and $UseLastItemKeyboardFallback) {
            Select-WallerComboBoxLastItemByKeyboard -Root $Root -AutomationId $AutomationId
        }

        if (-not $SkipSelectionVerification) {
            try {
                Wait-WallerComboBoxSelectionByName -Root $Root -AutomationId $AutomationId -Names $Names
            }
            catch {
                if (-not $UseLastItemKeyboardFallback) {
                    throw
                }

                Select-WallerComboBoxLastItemByKeyboard -Root $Root -AutomationId $AutomationId
                Wait-WallerComboBoxSelectionByName -Root $Root -AutomationId $AutomationId -Names $Names
            }
        }

        return
    }

    throw "UI Automation combo item not found for ${AutomationId}: $($Names -join ', ')."
}

function Select-WallerComboBoxLastItemByKeyboard {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    Add-Type -AssemblyName System.Windows.Forms

    $comboBox = Wait-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
    if (-not $comboBox.Current.IsEnabled) {
        throw "UI Automation element is disabled: $AutomationId."
    }

    $comboBox.SetFocus()
    Start-Sleep -Milliseconds 100
    [System.Windows.Forms.SendKeys]::SendWait("%{DOWN}")
    Start-Sleep -Milliseconds 150
    [System.Windows.Forms.SendKeys]::SendWait("{END}")
    Start-Sleep -Milliseconds 100
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Start-Sleep -Milliseconds 700
}

function Wait-WallerComboBoxSelectionByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId,
        [string[]]$Names,
        [int]$TimeoutSeconds = 5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastSelection = "<none>"
    while ((Get-Date) -lt $deadline) {
        $comboBox = Wait-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
        $selectionPattern = $null
        if (-not $comboBox.TryGetCurrentPattern(
            [System.Windows.Automation.SelectionPattern]::Pattern,
            [ref]$selectionPattern)) {
            return
        }

        $selectedItems = $selectionPattern.Current.GetSelection()
        $lastSelection = ($selectedItems | ForEach-Object { $_.Current.Name }) -join ", "
        if ([string]::IsNullOrWhiteSpace($lastSelection)) {
            $lastSelection = "<none>"
        }

        foreach ($selectedItem in $selectedItems) {
            if ($Names -contains $selectedItem.Current.Name) {
                return
            }
        }

        Start-Sleep -Milliseconds 150
    }

    throw "UI Automation combo selection did not reach expected item for ${AutomationId}: $($Names -join ', '). Current selection: $lastSelection."
}

function Wait-WallerSettingsJson {
    param(
        [string]$ExpectedTheme,
        [string]$ExpectedLanguage,
        [int]$TimeoutSeconds = 5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastSettingsText = "<missing>"
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $settingsPath) {
            try {
                $lastSettingsText = Get-Content -LiteralPath $settingsPath -Raw
                $settings = $lastSettingsText | ConvertFrom-Json
                if ($settings.Theme -eq $ExpectedTheme -and $settings.Language -eq $ExpectedLanguage) {
                    return $settings
                }
            }
            catch {
                $lastSettingsText = "<unreadable: $($_.Exception.Message)>"
            }
        }

        Start-Sleep -Milliseconds 150
    }

    throw "Settings JSON did not persist expected Theme=$ExpectedTheme and Language=$ExpectedLanguage. Last settings: $lastSettingsText"
}

Push-Location $nativeRoot
try {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes

    $buildArgs = @($ProjectPath, "-Detach")
    if ($DisableNuGetAudit) {
        $buildArgs += "-DisableNuGetAudit"
    }

    $output = powershell -ExecutionPolicy Bypass -File $buildScript @buildArgs 2>&1
    Assert-LastExitCode "Packaged launch"
    $text = $output | Out-String
    Write-Host $text

    $jsonMatch = [regex]::Match($text, "(?s)\{.*\}\s*$")
    if (-not $jsonMatch.Success) {
        throw "Launch output did not include trailing winapp JSON."
    }

    $launch = $jsonMatch.Value | ConvertFrom-Json
    if ($launch.Error) {
        throw "winapp launch failed: $($launch.Error)"
    }

    Set-WallerSettingsPathFromLaunch -Aumid $launch.AUMID
    Backup-WallerSettings

    if (-not $launch.ProcessId) {
        throw "Launch JSON did not include ProcessId."
    }

    $appProcessId = [int]$launch.ProcessId
    $deadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
    $process = $null
    $window = $null

    while ((Get-Date) -lt $deadline) {
        $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
        if ($process -and $process.MainWindowTitle) {
            $processCondition = [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
                $appProcessId)
            $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                [System.Windows.Automation.TreeScope]::Children,
                $processCondition)
            if ($window) {
                break
            }
        }

        Start-Sleep -Milliseconds 250
    }

    if (-not $process) {
        throw "Launched process $appProcessId was not found."
    }

    if ($process.ProcessName -ne "Waller.Native.App") {
        throw "Unexpected process name: $($process.ProcessName)."
    }

    if ($process.MainWindowTitle -ne "Waller") {
        throw "Unexpected main window title: $($process.MainWindowTitle)."
    }

    if (-not $process.Responding) {
        throw "Launched app is not responding."
    }

    if (-not $window) {
        throw "UI Automation window not found for launched process $appProcessId."
    }

    $requiredAutomationIds = @(
        "PresetComboBox",
        "SaveButton",
        "SaveAsButton",
        "ManagePresetsButton",
        "RefreshButton",
        "SettingsButton",
        "ApplyAllButton",
        "MonitorList",
        "StatusInfoBar"
    )

    [pscustomobject]@{
        ProcessId = $appProcessId
        Title = $process.MainWindowTitle
    } | Format-List | Out-String | Write-Host

    Assert-WallerElementsPresent `
        -Root $window `
        -AutomationIds $requiredAutomationIds `
        -ScopeName "Shell"

    Invoke-WallerElementByAutomationId -Root $window -AutomationId "SettingsButton"
    Assert-WallerElementsPresent `
        -Root $window `
        -AutomationIds @(
            "SettingsThemeComboBox",
            "SettingsLanguageComboBox",
            "ClearRenderedCacheButton",
            "SaveSettingsButton",
            "CloseSettingsButton") `
        -ScopeName "Settings modal"

    if ($SettingsRoundTrip) {
        Select-WallerComboBoxItemByName `
            -Root $window `
            -AutomationId "SettingsThemeComboBox" `
            -Names @("Light", "Claro")
        Select-WallerComboBoxItemByName `
            -Root $window `
            -AutomationId "SettingsLanguageComboBox" `
            -Names @("Spanish", "Espanol") `
            -UseLastItemKeyboardFallback `
            -SkipSelectionVerification
        Invoke-WallerElementByAutomationId -Root $window -AutomationId "SaveSettingsButton"
        $settings = Wait-WallerSettingsJson -ExpectedTheme "1" -ExpectedLanguage "es"
        [pscustomobject]@{
            SettingsRoundTrip = "Passed"
            Theme = $settings.Theme
            Language = $settings.Language
        } | Format-List | Out-String | Write-Host
    }

    Invoke-WallerElementByAutomationId -Root $window -AutomationId "CloseSettingsButton"

    Invoke-WallerElementByAutomationId -Root $window -AutomationId "SaveAsButton"
    Assert-WallerElementsPresent `
        -Root $window `
        -AutomationIds @(
            "SaveAsPresetNameTextBox",
            "ConfirmSaveAsButton",
            "CloseSaveAsButton") `
        -ScopeName "Save As modal"
    Invoke-WallerElementByAutomationId -Root $window -AutomationId "CloseSaveAsButton"

    Invoke-WallerElementByAutomationId -Root $window -AutomationId "ManagePresetsButton"
    Assert-WallerElementsPresent `
        -Root $window `
        -AutomationIds @(
            "ManagePresetList",
            "ManagePresetNameTextBox",
            "RenameManagedPresetButton",
            "DuplicateManagedPresetButton",
            "RequestDeleteManagedPresetButton",
            "CloseManagePresetsButton") `
        -ScopeName "Manage Presets modal"
    Invoke-WallerElementByAutomationId -Root $window -AutomationId "CloseManagePresetsButton"

    Stop-LaunchedApp -ProcessId $appProcessId

    Write-Host "SMOKE SURFACE PASSED: $appProcessId"
}
finally {
    if ($appProcessId) {
        Stop-LaunchedApp -ProcessId $appProcessId
    }

    Restore-WallerSettings
    Pop-Location
}
