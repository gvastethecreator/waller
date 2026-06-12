param(
    [string]$XamlPath = ".\Waller.Native.App"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedPath = if ([System.IO.Path]::IsPathRooted($XamlPath)) {
    $XamlPath
}
else {
    Join-Path $nativeRoot $XamlPath
}

if (-not (Test-Path -LiteralPath $resolvedPath)) {
    throw "XAML path not found: $resolvedPath"
}

$xamlFiles = if ((Get-Item -LiteralPath $resolvedPath).PSIsContainer) {
    Get-ChildItem -LiteralPath $resolvedPath -Recurse -Filter *.xaml |
        Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
        Sort-Object FullName
}
else {
    @(Get-Item -LiteralPath $resolvedPath)
}

if ($xamlFiles.Count -eq 0) {
    throw "No XAML files found: $resolvedPath"
}

$interactiveControls = @(
    "Button",
    "ComboBox",
    "ColorPicker",
    "ListView",
    "NumberBox",
    "TextBox"
)

$missingIds = @()
$missingNames = @()
$invalidIds = @()
$duplicateIds = @()
$textBoxesMissingPropertyChanged = @()
$hardCodedCornerRadii = @()
$hardCodedBackgroundColors = @()
$rowTemplatesMissingNames = @()
$modalBordersWithFixedWidth = @()
$modalBordersMissingMaxWidth = @()
$modalInteractiveControlsMissingTabIndex = @()
$modalInteractiveControlsWithInvalidTabIndex = @()
$modalInteractiveControlsWithDuplicateTabIndex = @()
$buttonsWithContentAttribute = @()
$buttonsMissingTooltips = @()
$fontIconsWithHardCodedFontSize = @()
$inlineButtonContentStacks = @()
$inlineSourcePreviewBindings = @()
$inlineCurrentMonitorRowActions = @()
$inlineMissingMonitorRowActions = @()
$missingMonitorActionButtonsNotCompact = @()
$rowActionButtonsMissingRowSpecificNames = @()
$inlineShellHeaderControls = @()
$inlineSaveAsModalControls = @()
$inlineSettingsModalControls = @()
$inlineManagePresetsModalControls = @()
$inlineEditPanelControls = @()
$inlineStatusFooterControls = @()
$inlineStatusFooterBindings = @()
$inlineTopologyStripBindings = @()
$inlineMonitorWorkspaceControls = @()
$inlineMonitorWorkspaceBindings = @()
$topologyTilesMissingNames = @()
$topologyResolutionLabelsMissingCompactBinding = @()
$statusFooterSurfacesMissingNames = @()
$statusFooterStatusInfoBarsNotPersistent = @()
$monitorWorkspaceMissingEmptyState = @()
$managePresetsMissingEmptyState = @()
$infoBarsMissingNames = @()
$infoBarsMissingLiveSettings = @()
$sourcePreviewsMissingNames = @()
$colorHexTextBoxesMissingFormatHints = @()
$editPanelMissingStableSourceHost = @()
$editPanelControlsWithUnexpectedTabIndex = @()

function Get-NativeRelativePath {
    param([string]$Path)

    $root = (Resolve-Path -LiteralPath $nativeRoot).Path.TrimEnd("\", "/")
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if ($resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($root.Length).TrimStart("\", "/")
    }

    return $resolved
}

function Get-NodeLabel {
    param(
        $Node,
        [string]$RelativePath
    )

    $name = $Node.GetAttribute("x:Name")
    $nodeLabel = if ([string]::IsNullOrWhiteSpace($name)) {
        $Node.LocalName
    }
    else {
        "$($Node.LocalName) '$name'"
    }

    return "${RelativePath}: $nodeLabel"
}

function Get-TemplateScope {
    param(
        $Node,
        [string]$RelativePath
    )

    $cursor = $Node.ParentNode
    while ($null -ne $cursor) {
        if ($cursor.LocalName -eq "DataTemplate") {
            return "${RelativePath}::DataTemplate:$([Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($cursor))"
        }

        $cursor = $cursor.ParentNode
    }

    return "${RelativePath}::Page"
}

function Get-FirstElementChild {
    param($Node)

    foreach ($child in $Node.ChildNodes) {
        if ($child.NodeType -eq [System.Xml.XmlNodeType]::Element) {
            return $child
        }
    }

    return $null
}

foreach ($file in $xamlFiles) {
    $relativePath = Get-NativeRelativePath $file.FullName
    $isMainPage = $relativePath -eq "Waller.Native.App\MainPage.xaml"
    $isTabIndexedModal = $relativePath -match "^Waller\.Native\.App\\Controls\\(ManagePresetsModal|SaveAsModal|SettingsModal)\.xaml$"
    $xamlText = Get-Content -LiteralPath $file.FullName -Raw
    $xaml = [xml]$xamlText
    $idsByScope = @{}
    $tabIndexesByModal = @{}

    foreach ($node in $xaml.SelectNodes("//*")) {
        if ($interactiveControls -contains $node.LocalName) {
            $automationId = $node.GetAttribute("AutomationProperties.AutomationId")
            if ([string]::IsNullOrWhiteSpace($automationId)) {
                $missingIds += (Get-NodeLabel $node $relativePath)
            }
            elseif ($automationId -notmatch "^[A-Za-z][A-Za-z0-9]*$") {
                $invalidIds += "${relativePath}: $($node.LocalName) '$automationId'"
            }
            else {
                $scope = Get-TemplateScope $node $relativePath
                $key = "$scope::$automationId"
                if ($idsByScope.ContainsKey($key)) {
                    $duplicateIds += "$automationId in $scope ($($idsByScope[$key]); $(Get-NodeLabel $node $relativePath))"
                }
                else {
                    $idsByScope[$key] = Get-NodeLabel $node $relativePath
                }
            }

            if ([string]::IsNullOrWhiteSpace($node.GetAttribute("AutomationProperties.Name"))) {
                $label = if ([string]::IsNullOrWhiteSpace($automationId)) {
                    Get-NodeLabel $node $relativePath
                }
                else {
                    "${relativePath}: $($node.LocalName) '$automationId'"
                }
                $missingNames += $label
            }

            if ($isMainPage) {
                if ($automationId -match "^(CloseSettingsButton|SettingsThemeComboBox|SettingsLanguageComboBox|ClearRenderedCacheButton|SaveSettingsButton)$") {
                    $inlineSettingsModalControls += (Get-NodeLabel $node $relativePath)
                }

                if ($automationId -match "^(CloseManagePresetsButton|ManagePresetList|ManagePresetNameTextBox|RenameManagedPresetButton|DuplicateManagedPresetButton|RequestDeleteManagedPresetButton|ConfirmDeleteManagedPresetButton)$") {
                    $inlineManagePresetsModalControls += (Get-NodeLabel $node $relativePath)
                }

                if ($automationId -match "^(SourceComboBox|ImagePathTextBox|ChooseImageButton|ColorHexTextBox|ColorPicker|ColorSwatchButton|FitComboBox|AnchorComboBox|OffsetXNumberBox|OffsetYNumberBox|ResetPositionButton)$") {
                    $inlineEditPanelControls += (Get-NodeLabel $node $relativePath)
                }

                if ($automationId -match "^CancelApplyButton$") {
                    $inlineStatusFooterControls += (Get-NodeLabel $node $relativePath)
                }

                if ($automationId -match "^MonitorList$") {
                    $inlineMonitorWorkspaceControls += (Get-NodeLabel $node $relativePath)
                }
            }

            if ($isTabIndexedModal) {
                $tabIndex = $node.GetAttribute("TabIndex")
                if ([string]::IsNullOrWhiteSpace($tabIndex)) {
                    $modalInteractiveControlsMissingTabIndex += (Get-NodeLabel $node $relativePath)
                }
                elseif ($tabIndex -notmatch "^\d+$") {
                    $modalInteractiveControlsWithInvalidTabIndex += "$(Get-NodeLabel $node $relativePath): '$tabIndex'"
                }
                elseif ($tabIndexesByModal.ContainsKey($tabIndex)) {
                    $modalInteractiveControlsWithDuplicateTabIndex += "TabIndex $tabIndex in $relativePath ($($tabIndexesByModal[$tabIndex]); $(Get-NodeLabel $node $relativePath))"
                }
                else {
                    $tabIndexesByModal[$tabIndex] = Get-NodeLabel $node $relativePath
                }
            }
        }

        if ($node.LocalName -eq "Button" -and -not [string]::IsNullOrWhiteSpace($node.GetAttribute("Content"))) {
            $buttonsWithContentAttribute += (Get-NodeLabel $node $relativePath)
        }

        if ($node.LocalName -eq "Button" -and [string]::IsNullOrWhiteSpace($node.GetAttribute("ToolTipService.ToolTip"))) {
            $buttonsMissingTooltips += (Get-NodeLabel $node $relativePath)
        }

        if ($node.LocalName -eq "Button" -and $isMainPage) {
            $automationId = $node.GetAttribute("AutomationProperties.AutomationId")
            if ($automationId -match "^Monitor(Edit|Apply)Button$") {
                $inlineCurrentMonitorRowActions += (Get-NodeLabel $node $relativePath)
            }

            if ($automationId -match "^MissingMonitor(Reassign|Forget)Button$") {
                $inlineMissingMonitorRowActions += (Get-NodeLabel $node $relativePath)
            }

            if ($automationId -match "^(PresetComboBox|SaveButton|SaveAsButton|ManagePresetsButton|RefreshButton|SettingsButton|ApplyAllButton)$") {
                $inlineShellHeaderControls += (Get-NodeLabel $node $relativePath)
            }

            if ($automationId -match "^(CloseSaveAsButton|SaveAsPresetNameTextBox|ConfirmSaveAsButton)$") {
                $inlineSaveAsModalControls += (Get-NodeLabel $node $relativePath)
            }
        }

        if ($node.LocalName -eq "FontIcon" -and -not [string]::IsNullOrWhiteSpace($node.GetAttribute("FontSize"))) {
            $fontIconsWithHardCodedFontSize += (Get-NodeLabel $node $relativePath)
        }

        if ($node.LocalName -eq "InfoBar") {
            if ([string]::IsNullOrWhiteSpace($node.GetAttribute("AutomationProperties.Name"))) {
                $infoBarsMissingNames += (Get-NodeLabel $node $relativePath)
            }

            if ([string]::IsNullOrWhiteSpace($node.GetAttribute("AutomationProperties.LiveSetting"))) {
                $infoBarsMissingLiveSettings += (Get-NodeLabel $node $relativePath)
            }
        }

        if ($node.LocalName -eq "SourcePreview" -or
            ($relativePath -eq "Waller.Native.App\Controls\SourcePreview.xaml" `
                -and $node.LocalName -eq "Border" `
                -and $node.GetAttribute("Background") -like "*PreviewBrush*")) {
            if ([string]::IsNullOrWhiteSpace($node.GetAttribute("AutomationProperties.Name"))) {
                $sourcePreviewsMissingNames += (Get-NodeLabel $node $relativePath)
            }
        }

        if ($relativePath -ne "Waller.Native.App\Controls\IconText.xaml" `
            -and $node.LocalName -eq "StackPanel" `
            -and $node.GetAttribute("Style") -eq "{StaticResource WallerButtonContentStackStyle}") {
            $inlineButtonContentStacks += (Get-NodeLabel $node $relativePath)
        }

        if ($node.LocalName -ne "SourcePreview") {
            foreach ($attribute in $node.Attributes) {
                if ($attribute.Value -like "*SourcePreviewBrush*") {
                    $inlineSourcePreviewBindings += (Get-NodeLabel $node $relativePath)
                    break
                }
            }
        }

        if ($isMainPage -and $node.LocalName -ne "StatusFooter") {
            foreach ($attribute in $node.Attributes) {
                if ($attribute.Value -like "*StatusText*" -or $attribute.Value -like "*ApplyProgressText*") {
                    $inlineStatusFooterBindings += (Get-NodeLabel $node $relativePath)
                    break
                }
            }
        }

        if ($isMainPage -and $node.LocalName -ne "TopologyStrip") {
            foreach ($attribute in $node.Attributes) {
                if ($attribute.Value -like "*TopologyVisibility*" -or $attribute.Value -like "*TopologyWidth*" -or $attribute.Value -like "*TopologyHeight*") {
                    $inlineTopologyStripBindings += (Get-NodeLabel $node $relativePath)
                    break
                }
            }
        }

        if ($isMainPage -and $node.LocalName -ne "MonitorWorkspace") {
            foreach ($attribute in $node.Attributes) {
                if ($attribute.Value -like "*MissingMonitorsVisibility*" `
                    -or $attribute.Value -like "*NoMonitorsVisibility*" `
                    -or $attribute.Value -like "*NoMonitorsDetected*") {
                    $inlineMonitorWorkspaceBindings += (Get-NodeLabel $node $relativePath)
                    break
                }
            }
        }

        if ($node.LocalName -eq "TextBox") {
            $textBinding = $node.GetAttribute("Text")
            if ($textBinding -like "*Mode=TwoWay*" -and $textBinding -notlike "*UpdateSourceTrigger=PropertyChanged*") {
                $name = $node.GetAttribute("x:Name")
                $automationId = $node.GetAttribute("AutomationProperties.AutomationId")
                $labelName = if ([string]::IsNullOrWhiteSpace($name)) { $automationId } else { $name }
                $label = if ([string]::IsNullOrWhiteSpace($labelName)) { "TextBox" } else { "TextBox '$labelName'" }
                $textBoxesMissingPropertyChanged += "${relativePath}: $label"
            }

            if ($node.GetAttribute("AutomationProperties.AutomationId") -eq "ColorHexTextBox" `
                -and ($node.GetAttribute("MaxLength") -ne "7" `
                    -or $node.GetAttribute("PlaceholderText") -ne "#RRGGBB")) {
                $colorHexTextBoxesMissingFormatHints += (Get-NodeLabel $node $relativePath)
            }
        }
    }

    foreach ($template in $xaml.SelectNodes("//*[local-name()='DataTemplate']")) {
        $containsRowAction = $false
        foreach ($button in $template.SelectNodes(".//*[local-name()='Button']")) {
            $automationId = $button.GetAttribute("AutomationProperties.AutomationId")
            if ($automationId -match "^(Monitor|MissingMonitor).+Button$") {
                $containsRowAction = $true
                break
            }
        }

        if (-not $containsRowAction) {
            continue
        }

        $root = Get-FirstElementChild $template
        if ($null -eq $root -or [string]::IsNullOrWhiteSpace($root.GetAttribute("AutomationProperties.Name"))) {
            $rowTemplatesMissingNames += "${relativePath}: DataTemplate:$([Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($template))"
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\TopologyStrip.xaml") {
        foreach ($template in $xaml.SelectNodes("//*[local-name()='DataTemplate']")) {
            $root = Get-FirstElementChild $template
            if ($null -eq $root -or [string]::IsNullOrWhiteSpace($root.GetAttribute("AutomationProperties.Name"))) {
                $topologyTilesMissingNames += "${relativePath}: DataTemplate:$([Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($template))"
            }
        }

        $hasCompactResolutionLabel = $false
        foreach ($textBlock in $xaml.SelectNodes("//*[local-name()='TextBlock']")) {
            if ($textBlock.GetAttribute("Text") -like "*Resolution*" -and
                $textBlock.GetAttribute("Visibility") -like "*TopologyResolutionVisibility*" -and
                $textBlock.GetAttribute("TextTrimming") -eq "CharacterEllipsis") {
                $hasCompactResolutionLabel = $true
                break
            }
        }

        if (-not $hasCompactResolutionLabel) {
            $topologyResolutionLabelsMissingCompactBinding += "${relativePath}: Resolution"
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\StatusFooter.xaml") {
        foreach ($automationId in @("StatusInfoBar", "ApplyProgressRing", "ApplyProgressText")) {
            $surface = $xaml.SelectSingleNode("//*[@AutomationProperties.AutomationId='$automationId']")
            if ($null -eq $surface -or [string]::IsNullOrWhiteSpace($surface.GetAttribute("AutomationProperties.Name"))) {
                $statusFooterSurfacesMissingNames += "${relativePath}: $automationId"
            }
        }

        $statusInfoBar = $xaml.SelectSingleNode("//*[@AutomationProperties.AutomationId='StatusInfoBar']")
        if ($null -eq $statusInfoBar -or $statusInfoBar.GetAttribute("IsOpen") -ne "True") {
            $statusFooterStatusInfoBarsNotPersistent += "${relativePath}: StatusInfoBar"
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\EditPanel.xaml") {
        $sourceEditorHost = $null
        $expectedEditPanelTabIndexes = @{
            SourceComboBox = "0"
            ImagePathTextBox = "1"
            ChooseImageButton = "2"
            ColorHexTextBox = "3"
            ColorPicker = "4"
            ColorSwatchButton = "5"
            FitComboBox = "6"
            AnchorComboBox = "7"
            OffsetXNumberBox = "8"
            OffsetYNumberBox = "9"
            ResetPositionButton = "10"
        }
        $seenEditPanelTabIndexes = @{}

        foreach ($node in $xaml.SelectNodes("//*")) {
            if ($node.GetAttribute("x:Name") -eq "SourceEditorHost") {
                $sourceEditorHost = $node
            }

            $automationId = $node.GetAttribute("AutomationProperties.AutomationId")
            if ($expectedEditPanelTabIndexes.ContainsKey($automationId)) {
                $seenEditPanelTabIndexes[$automationId] = $true
                $expectedTabIndex = $expectedEditPanelTabIndexes[$automationId]
                $actualTabIndex = $node.GetAttribute("TabIndex")
                if ($actualTabIndex -ne $expectedTabIndex) {
                    $editPanelControlsWithUnexpectedTabIndex += "$(Get-NodeLabel $node $relativePath): $automationId expected $expectedTabIndex, found '$actualTabIndex'"
                }
            }
        }

        foreach ($automationId in $expectedEditPanelTabIndexes.Keys) {
            if (-not $seenEditPanelTabIndexes.ContainsKey($automationId)) {
                $editPanelControlsWithUnexpectedTabIndex += "${relativePath}: $automationId missing from edit panel tab order"
            }
        }

        if ($null -eq $sourceEditorHost -or
            $sourceEditorHost.LocalName -ne "ScrollViewer" -or
            $sourceEditorHost.GetAttribute("MinHeight") -ne "320" -or
            $sourceEditorHost.GetAttribute("MaxHeight") -ne "320" -or
            $sourceEditorHost.GetAttribute("VerticalScrollBarVisibility") -ne "Auto") {
            $editPanelMissingStableSourceHost += "${relativePath}: SourceEditorHost"
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\MonitorRow.xaml") {
        $expectedRowActionNames = @{
            MonitorEditButton = "Row.EditAccessibleName"
            MonitorApplyButton = "Row.ApplyAccessibleName"
        }

        foreach ($button in $xaml.SelectNodes("//*[local-name()='Button']")) {
            $automationId = $button.GetAttribute("AutomationProperties.AutomationId")
            if (-not $expectedRowActionNames.ContainsKey($automationId)) {
                continue
            }

            if ($button.GetAttribute("AutomationProperties.Name") -notlike "*$($expectedRowActionNames[$automationId])*") {
                $rowActionButtonsMissingRowSpecificNames += "$(Get-NodeLabel $button $relativePath): $automationId"
            }
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\MonitorWorkspace.xaml") {
        $hasNoMonitorText = $false
        foreach ($textBlock in $xaml.SelectNodes("//*[local-name()='TextBlock']")) {
            if ($textBlock.GetAttribute("Text") -like "*Text.NoMonitorsDetected*" -and
                $textBlock.GetAttribute("Visibility") -like "*NoMonitorsVisibility*") {
                $hasNoMonitorText = $true
                break
            }
        }

        if (-not $hasNoMonitorText) {
            $monitorWorkspaceMissingEmptyState += "${relativePath}: NoMonitorsDetected"
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\ManagePresetsModal.xaml") {
        $hasNoPresetText = $false
        foreach ($textBlock in $xaml.SelectNodes("//*[local-name()='TextBlock']")) {
            if ($textBlock.GetAttribute("Text") -like "*Text.NoPresetsSaved*" -and
                $textBlock.GetAttribute("Visibility") -like "*ManagePresetEmptyVisibility*") {
                $hasNoPresetText = $true
                break
            }
        }

        if (-not $hasNoPresetText) {
            $managePresetsMissingEmptyState += "${relativePath}: NoPresetsSaved"
        }
    }

    if ($relativePath -eq "Waller.Native.App\Controls\MissingMonitorRow.xaml") {
        $expectedMissingRowActionNames = @{
            MissingMonitorReassignButton = "Row.ReassignAccessibleName"
            MissingMonitorForgetButton = "Row.ForgetAccessibleName"
        }

        foreach ($button in $xaml.SelectNodes("//*[local-name()='Button']")) {
            $automationId = $button.GetAttribute("AutomationProperties.AutomationId")
            if ($automationId -notmatch "^MissingMonitor(Reassign|Forget)Button$") {
                continue
            }

            if ($button.GetAttribute("AutomationProperties.Name") -notlike "*$($expectedMissingRowActionNames[$automationId])*") {
                $rowActionButtonsMissingRowSpecificNames += "$(Get-NodeLabel $button $relativePath): $automationId"
            }

            $hasFontIconChild = $false
            foreach ($child in $button.ChildNodes) {
                if ($child.NodeType -eq [System.Xml.XmlNodeType]::Element -and $child.LocalName -eq "FontIcon") {
                    $hasFontIconChild = $true
                    break
                }
            }

            if ($button.GetAttribute("Width") -ne "40" -or
                $button.GetAttribute("Height") -ne "32" -or
                $button.GetAttribute("Padding") -ne "0" -or
                -not $hasFontIconChild) {
                $missingMonitorActionButtonsNotCompact += (Get-NodeLabel $button $relativePath)
            }
        }
    }

    foreach ($border in $xaml.SelectNodes("//*[local-name()='Border']")) {
        $parent = $border.ParentNode
        if ($null -eq $parent -or $parent.LocalName -ne "Grid") {
            continue
        }

        $isModalOverlay = $parent.GetAttribute("Grid.RowSpan") -eq "4" -or
            $parent.GetAttribute("Background") -like "*WallerModalOverlayBrush*"
        if (-not $isModalOverlay) {
            continue
        }

        $label = Get-NodeLabel $border $relativePath
        if (-not [string]::IsNullOrWhiteSpace($border.GetAttribute("Width"))) {
            $modalBordersWithFixedWidth += $label
        }

        if ([string]::IsNullOrWhiteSpace($border.GetAttribute("MaxWidth"))) {
            $modalBordersMissingMaxWidth += $label
        }
    }

    $lineNumber = 0
    foreach ($line in $xamlText -split "`r?`n") {
        $lineNumber++
        if ($line -match 'CornerRadius="\d') {
            $hardCodedCornerRadii += "${relativePath}: line $lineNumber`: $($line.Trim())"
        }

        if ($line -match 'Background="#') {
            $hardCodedBackgroundColors += "${relativePath}: line $lineNumber`: $($line.Trim())"
        }
    }
}

if ($missingIds.Count -gt 0) {
    Write-Host "Missing AutomationProperties.AutomationId:" -ForegroundColor Red
    foreach ($missingId in $missingIds) {
        Write-Host " - $missingId" -ForegroundColor Red
    }
}

if ($invalidIds.Count -gt 0) {
    Write-Host "Invalid AutomationProperties.AutomationId tokens:" -ForegroundColor Red
    foreach ($invalidId in $invalidIds) {
        Write-Host " - $invalidId" -ForegroundColor Red
    }
}

if ($missingNames.Count -gt 0) {
    Write-Host "Interactive controls missing AutomationProperties.Name:" -ForegroundColor Red
    foreach ($missingName in $missingNames) {
        Write-Host " - $missingName" -ForegroundColor Red
    }
}

if ($duplicateIds.Count -gt 0) {
    Write-Host "Duplicate AutomationProperties.AutomationId values in the same XAML scope:" -ForegroundColor Red
    foreach ($duplicateId in $duplicateIds) {
        Write-Host " - $duplicateId" -ForegroundColor Red
    }
}

if ($textBoxesMissingPropertyChanged.Count -gt 0) {
    Write-Host "TextBox TwoWay binding missing UpdateSourceTrigger=PropertyChanged:" -ForegroundColor Red
    foreach ($textBox in $textBoxesMissingPropertyChanged) {
        Write-Host " - $textBox" -ForegroundColor Red
    }
}

if ($hardCodedCornerRadii.Count -gt 0) {
    Write-Host "Hard-coded CornerRadius values found; use WinUI theme resources:" -ForegroundColor Red
    foreach ($cornerRadius in $hardCodedCornerRadii) {
        Write-Host " - $cornerRadius" -ForegroundColor Red
    }
}

if ($hardCodedBackgroundColors.Count -gt 0) {
    Write-Host "Hard-coded Background colors found; use ThemeResource brushes:" -ForegroundColor Red
    foreach ($backgroundColor in $hardCodedBackgroundColors) {
        Write-Host " - $backgroundColor" -ForegroundColor Red
    }
}

if ($rowTemplatesMissingNames.Count -gt 0) {
    Write-Host "Monitor row DataTemplate roots missing AutomationProperties.Name:" -ForegroundColor Red
    foreach ($template in $rowTemplatesMissingNames) {
        Write-Host " - $template" -ForegroundColor Red
    }
}

if ($modalBordersWithFixedWidth.Count -gt 0) {
    Write-Host "Modal overlay borders with fixed Width found; use MaxWidth plus stretch/margin for narrow windows:" -ForegroundColor Red
    foreach ($border in $modalBordersWithFixedWidth) {
        Write-Host " - $border" -ForegroundColor Red
    }
}

if ($topologyTilesMissingNames.Count -gt 0) {
    Write-Host "Topology tile roots missing AutomationProperties.Name:" -ForegroundColor Red
    foreach ($tile in $topologyTilesMissingNames) {
        Write-Host " - $tile" -ForegroundColor Red
    }
}

if ($topologyResolutionLabelsMissingCompactBinding.Count -gt 0) {
    Write-Host "Topology resolution labels missing compact visibility/trimming binding; avoid cramped text inside tiny monitor tiles:" -ForegroundColor Red
    foreach ($label in $topologyResolutionLabelsMissingCompactBinding) {
        Write-Host " - $label" -ForegroundColor Red
    }
}

if ($statusFooterSurfacesMissingNames.Count -gt 0) {
    Write-Host "Status footer surfaces missing AutomationProperties.Name:" -ForegroundColor Red
    foreach ($surface in $statusFooterSurfacesMissingNames) {
        Write-Host " - $surface" -ForegroundColor Red
    }
}

if ($statusFooterStatusInfoBarsNotPersistent.Count -gt 0) {
    Write-Host "Status footer InfoBar is not persistent; keep StatusInfoBar IsOpen=True so final operation status remains visible:" -ForegroundColor Red
    foreach ($surface in $statusFooterStatusInfoBarsNotPersistent) {
        Write-Host " - $surface" -ForegroundColor Red
    }
}

if ($monitorWorkspaceMissingEmptyState.Count -gt 0) {
    Write-Host "Monitor workspace missing no-monitors empty text bound to NoMonitorsVisibility:" -ForegroundColor Red
    foreach ($state in $monitorWorkspaceMissingEmptyState) {
        Write-Host " - $state" -ForegroundColor Red
    }
}

if ($managePresetsMissingEmptyState.Count -gt 0) {
    Write-Host "Manage Presets modal missing no-presets empty text bound to ManagePresetEmptyVisibility:" -ForegroundColor Red
    foreach ($state in $managePresetsMissingEmptyState) {
        Write-Host " - $state" -ForegroundColor Red
    }
}

if ($infoBarsMissingNames.Count -gt 0) {
    Write-Host "InfoBars missing AutomationProperties.Name found; expose warning/status text to assistive tech:" -ForegroundColor Red
    foreach ($infoBar in $infoBarsMissingNames) {
        Write-Host " - $infoBar" -ForegroundColor Red
    }
}

if ($infoBarsMissingLiveSettings.Count -gt 0) {
    Write-Host "InfoBars missing AutomationProperties.LiveSetting found; status/warning updates should be announced:" -ForegroundColor Red
    foreach ($infoBar in $infoBarsMissingLiveSettings) {
        Write-Host " - $infoBar" -ForegroundColor Red
    }
}

if ($sourcePreviewsMissingNames.Count -gt 0) {
    Write-Host "Source previews missing AutomationProperties.Name found; preview meaning should be available to assistive tech:" -ForegroundColor Red
    foreach ($preview in $sourcePreviewsMissingNames) {
        Write-Host " - $preview" -ForegroundColor Red
    }
}

if ($colorHexTextBoxesMissingFormatHints.Count -gt 0) {
    Write-Host "Color hex TextBox missing #RRGGBB format hint or MaxLength=7:" -ForegroundColor Red
    foreach ($textBox in $colorHexTextBoxesMissingFormatHints) {
        Write-Host " - $textBox" -ForegroundColor Red
    }
}

if ($editPanelMissingStableSourceHost.Count -gt 0) {
    Write-Host "Edit panel missing stable SourceEditorHost; keep source-specific editors in fixed-height scroll host to avoid placement layout jumps on monitor selection:" -ForegroundColor Red
    foreach ($host in $editPanelMissingStableSourceHost) {
        Write-Host " - $host" -ForegroundColor Red
    }
}

if ($editPanelControlsWithUnexpectedTabIndex.Count -gt 0) {
    Write-Host "Edit panel controls missing expected TabIndex order; keep keyboard editing sequence source -> source details -> placement:" -ForegroundColor Red
    foreach ($control in $editPanelControlsWithUnexpectedTabIndex) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($rowActionButtonsMissingRowSpecificNames.Count -gt 0) {
    Write-Host "Monitor row action buttons missing row-specific AutomationProperties.Name values; avoid repeated generic Edit/Apply/Reassign/Forget announcements:" -ForegroundColor Red
    foreach ($button in $rowActionButtonsMissingRowSpecificNames) {
        Write-Host " - $button" -ForegroundColor Red
    }
}

if ($modalBordersMissingMaxWidth.Count -gt 0) {
    Write-Host "Modal overlay borders missing MaxWidth found; constrain modal width responsively:" -ForegroundColor Red
    foreach ($border in $modalBordersMissingMaxWidth) {
        Write-Host " - $border" -ForegroundColor Red
    }
}

if ($modalInteractiveControlsMissingTabIndex.Count -gt 0) {
    Write-Host "Modal interactive controls missing TabIndex found; keep keyboard traversal deterministic:" -ForegroundColor Red
    foreach ($control in $modalInteractiveControlsMissingTabIndex) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($modalInteractiveControlsWithInvalidTabIndex.Count -gt 0) {
    Write-Host "Modal interactive controls with invalid TabIndex found; use non-negative integer values:" -ForegroundColor Red
    foreach ($control in $modalInteractiveControlsWithInvalidTabIndex) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($modalInteractiveControlsWithDuplicateTabIndex.Count -gt 0) {
    Write-Host "Modal interactive controls with duplicate TabIndex found; keep keyboard traversal deterministic:" -ForegroundColor Red
    foreach ($control in $modalInteractiveControlsWithDuplicateTabIndex) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($buttonsWithContentAttribute.Count -gt 0) {
    Write-Host "Button Content attributes found; use explicit icon/text child content plus AutomationProperties.Name:" -ForegroundColor Red
    foreach ($button in $buttonsWithContentAttribute) {
        Write-Host " - $button" -ForegroundColor Red
    }
}

if ($buttonsMissingTooltips.Count -gt 0) {
    Write-Host "Buttons missing ToolTipService.ToolTip found; keep command hints available for mouse users:" -ForegroundColor Red
    foreach ($button in $buttonsMissingTooltips) {
        Write-Host " - $button" -ForegroundColor Red
    }
}

if ($fontIconsWithHardCodedFontSize.Count -gt 0) {
    Write-Host "FontIcon hard-coded FontSize values found; use WallerButtonIconStyle:" -ForegroundColor Red
    foreach ($fontIcon in $fontIconsWithHardCodedFontSize) {
        Write-Host " - $fontIcon" -ForegroundColor Red
    }
}

if ($inlineButtonContentStacks.Count -gt 0) {
    Write-Host "Inline button icon/text stacks found; use Controls/IconText.xaml:" -ForegroundColor Red
    foreach ($stack in $inlineButtonContentStacks) {
        Write-Host " - $stack" -ForegroundColor Red
    }
}

if ($inlineSourcePreviewBindings.Count -gt 0) {
    Write-Host "Inline source-preview bindings found; use Controls/SourcePreview.xaml:" -ForegroundColor Red
    foreach ($preview in $inlineSourcePreviewBindings) {
        Write-Host " - $preview" -ForegroundColor Red
    }
}

if ($inlineCurrentMonitorRowActions.Count -gt 0) {
    Write-Host "Inline current-monitor row action buttons found; use Controls/MonitorRow.xaml:" -ForegroundColor Red
    foreach ($button in $inlineCurrentMonitorRowActions) {
        Write-Host " - $button" -ForegroundColor Red
    }
}

if ($inlineMissingMonitorRowActions.Count -gt 0) {
    Write-Host "Inline missing-monitor row action buttons found; use Controls/MissingMonitorRow.xaml:" -ForegroundColor Red
    foreach ($button in $inlineMissingMonitorRowActions) {
        Write-Host " - $button" -ForegroundColor Red
    }
}

if ($missingMonitorActionButtonsNotCompact.Count -gt 0) {
    Write-Host "Missing-monitor row action buttons are not compact icon-only buttons; keep stale-monitor rows narrow and use tooltips/accessibility names for labels:" -ForegroundColor Red
    foreach ($button in $missingMonitorActionButtonsNotCompact) {
        Write-Host " - $button" -ForegroundColor Red
    }
}

if ($inlineShellHeaderControls.Count -gt 0) {
    Write-Host "Inline shell header controls found; use Controls/ShellHeader.xaml:" -ForegroundColor Red
    foreach ($control in $inlineShellHeaderControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineSaveAsModalControls.Count -gt 0) {
    Write-Host "Inline Save As modal controls found; use Controls/SaveAsModal.xaml:" -ForegroundColor Red
    foreach ($control in $inlineSaveAsModalControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineSettingsModalControls.Count -gt 0) {
    Write-Host "Inline Settings modal controls found; use Controls/SettingsModal.xaml:" -ForegroundColor Red
    foreach ($control in $inlineSettingsModalControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineManagePresetsModalControls.Count -gt 0) {
    Write-Host "Inline Manage Presets modal controls found; use Controls/ManagePresetsModal.xaml:" -ForegroundColor Red
    foreach ($control in $inlineManagePresetsModalControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineEditPanelControls.Count -gt 0) {
    Write-Host "Inline edit-panel controls found; use Controls/EditPanel.xaml:" -ForegroundColor Red
    foreach ($control in $inlineEditPanelControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineStatusFooterControls.Count -gt 0) {
    Write-Host "Inline status-footer controls found; use Controls/StatusFooter.xaml:" -ForegroundColor Red
    foreach ($control in $inlineStatusFooterControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineStatusFooterBindings.Count -gt 0) {
    Write-Host "Inline status-footer bindings found; use Controls/StatusFooter.xaml:" -ForegroundColor Red
    foreach ($binding in $inlineStatusFooterBindings) {
        Write-Host " - $binding" -ForegroundColor Red
    }
}

if ($inlineTopologyStripBindings.Count -gt 0) {
    Write-Host "Inline topology-strip bindings found; use Controls/TopologyStrip.xaml:" -ForegroundColor Red
    foreach ($binding in $inlineTopologyStripBindings) {
        Write-Host " - $binding" -ForegroundColor Red
    }
}

if ($inlineMonitorWorkspaceControls.Count -gt 0) {
    Write-Host "Inline monitor-workspace controls found; use Controls/MonitorWorkspace.xaml:" -ForegroundColor Red
    foreach ($control in $inlineMonitorWorkspaceControls) {
        Write-Host " - $control" -ForegroundColor Red
    }
}

if ($inlineMonitorWorkspaceBindings.Count -gt 0) {
    Write-Host "Inline monitor-workspace bindings found; use Controls/MonitorWorkspace.xaml:" -ForegroundColor Red
    foreach ($binding in $inlineMonitorWorkspaceBindings) {
        Write-Host " - $binding" -ForegroundColor Red
    }
}

if ($missingIds.Count -gt 0 `
    -or $missingNames.Count -gt 0 `
    -or $invalidIds.Count -gt 0 `
    -or $duplicateIds.Count -gt 0 `
    -or $textBoxesMissingPropertyChanged.Count -gt 0 `
    -or $hardCodedCornerRadii.Count -gt 0 `
    -or $hardCodedBackgroundColors.Count -gt 0 `
    -or $rowTemplatesMissingNames.Count -gt 0 `
    -or $modalBordersWithFixedWidth.Count -gt 0 `
    -or $topologyTilesMissingNames.Count -gt 0 `
    -or $topologyResolutionLabelsMissingCompactBinding.Count -gt 0 `
    -or $statusFooterSurfacesMissingNames.Count -gt 0 `
    -or $statusFooterStatusInfoBarsNotPersistent.Count -gt 0 `
    -or $monitorWorkspaceMissingEmptyState.Count -gt 0 `
    -or $managePresetsMissingEmptyState.Count -gt 0 `
    -or $infoBarsMissingNames.Count -gt 0 `
    -or $infoBarsMissingLiveSettings.Count -gt 0 `
    -or $sourcePreviewsMissingNames.Count -gt 0 `
    -or $colorHexTextBoxesMissingFormatHints.Count -gt 0 `
    -or $editPanelMissingStableSourceHost.Count -gt 0 `
    -or $editPanelControlsWithUnexpectedTabIndex.Count -gt 0 `
    -or $modalBordersMissingMaxWidth.Count -gt 0 `
    -or $modalInteractiveControlsMissingTabIndex.Count -gt 0 `
    -or $modalInteractiveControlsWithInvalidTabIndex.Count -gt 0 `
    -or $modalInteractiveControlsWithDuplicateTabIndex.Count -gt 0 `
    -or $buttonsWithContentAttribute.Count -gt 0 `
    -or $buttonsMissingTooltips.Count -gt 0 `
    -or $fontIconsWithHardCodedFontSize.Count -gt 0 `
    -or $inlineButtonContentStacks.Count -gt 0 `
    -or $inlineSourcePreviewBindings.Count -gt 0 `
    -or $inlineCurrentMonitorRowActions.Count -gt 0 `
    -or $inlineMissingMonitorRowActions.Count -gt 0 `
    -or $missingMonitorActionButtonsNotCompact.Count -gt 0 `
    -or $rowActionButtonsMissingRowSpecificNames.Count -gt 0 `
    -or $inlineShellHeaderControls.Count -gt 0 `
    -or $inlineSaveAsModalControls.Count -gt 0 `
    -or $inlineSettingsModalControls.Count -gt 0 `
    -or $inlineManagePresetsModalControls.Count -gt 0 `
    -or $inlineEditPanelControls.Count -gt 0 `
    -or $inlineStatusFooterControls.Count -gt 0 `
    -or $inlineStatusFooterBindings.Count -gt 0 `
    -or $inlineTopologyStripBindings.Count -gt 0 `
    -or $inlineMonitorWorkspaceControls.Count -gt 0 `
    -or $inlineMonitorWorkspaceBindings.Count -gt 0) {
    exit 1
}

Write-Host "XAML accessibility/theme lint passed for $($xamlFiles.Count) file(s)."
