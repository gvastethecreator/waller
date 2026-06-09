param(
    [string]$XamlPath = ".\Waller.Native.App\MainPage.xaml"
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
    throw "XAML file not found: $resolvedPath"
}

$xamlText = Get-Content -LiteralPath $resolvedPath -Raw

$interactiveControls = @(
    "Button",
    "ComboBox",
    "ColorPicker",
    "ListView",
    "NumberBox",
    "TextBox"
)

$xaml = [xml]$xamlText
$missingIds = @()
$invalidIds = @()
$duplicateIds = @()
$textBoxesMissingPropertyChanged = @()
$hardCodedCornerRadii = @()
$hardCodedBackgroundColors = @()
$rowTemplatesMissingNames = @()
$idsByScope = @{}

function Get-NodeLabel {
    param($Node)

    $name = $Node.GetAttribute("x:Name")
    if ([string]::IsNullOrWhiteSpace($name)) {
        return $Node.LocalName
    }

    return "$($Node.LocalName) '$name'"
}

function Get-TemplateScope {
    param($Node)

    $cursor = $Node.ParentNode
    while ($null -ne $cursor) {
        if ($cursor.LocalName -eq "DataTemplate") {
            return "DataTemplate:$([Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($cursor))"
        }

        $cursor = $cursor.ParentNode
    }

    return "Page"
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

foreach ($node in $xaml.SelectNodes("//*")) {
    if ($interactiveControls -contains $node.LocalName) {
        $automationId = $node.GetAttribute("AutomationProperties.AutomationId")
        if ([string]::IsNullOrWhiteSpace($automationId)) {
            $missingIds += (Get-NodeLabel $node)
        }
        elseif ($automationId -notmatch "^[A-Za-z][A-Za-z0-9]*$") {
            $invalidIds += "$($node.LocalName) '$automationId'"
        }
        else {
            $scope = Get-TemplateScope $node
            $key = "$scope::$automationId"
            if ($idsByScope.ContainsKey($key)) {
                $duplicateIds += "$automationId in $scope ($($idsByScope[$key]); $(Get-NodeLabel $node))"
            }
            else {
                $idsByScope[$key] = Get-NodeLabel $node
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
            $textBoxesMissingPropertyChanged += $label
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
        $rowTemplatesMissingNames += "DataTemplate:$([Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($template))"
    }
}

$lineNumber = 0
foreach ($line in $xamlText -split "`r?`n") {
    $lineNumber++
    if ($line -match 'CornerRadius="\d') {
        $hardCodedCornerRadii += "line $lineNumber`: $($line.Trim())"
    }

    if ($line -match 'Background="#') {
        $hardCodedBackgroundColors += "line $lineNumber`: $($line.Trim())"
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

if ($duplicateIds.Count -gt 0) {
    Write-Host "Duplicate AutomationProperties.AutomationId values in the same XAML scope:" -ForegroundColor Red
    foreach ($duplicateId in $duplicateIds) {
        Write-Host " - $duplicateId" -ForegroundColor Red
    }
}

if ($textBoxesMissingPropertyChanged.Count -gt 0) {
    Write-Host "TextBox TwoWay x:Bind missing UpdateSourceTrigger=PropertyChanged:" -ForegroundColor Red
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

if ($missingIds.Count -gt 0 `
    -or $invalidIds.Count -gt 0 `
    -or $duplicateIds.Count -gt 0 `
    -or $textBoxesMissingPropertyChanged.Count -gt 0 `
    -or $hardCodedCornerRadii.Count -gt 0 `
    -or $hardCodedBackgroundColors.Count -gt 0 `
    -or $rowTemplatesMissingNames.Count -gt 0) {
    exit 1
}

Write-Host "XAML accessibility/theme lint passed."
