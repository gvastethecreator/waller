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
$xaml = [xml]$xamlText
$hardCodedUserText = @()

$localizedTextAttributes = @(
    "AutomationProperties.Name",
    "Content",
    "Header",
    "Message",
    "Text",
    "ToolTipService.ToolTip"
)

function Get-NodeLabel {
    param($Node)

    $name = $Node.GetAttribute("x:Name")
    if ([string]::IsNullOrWhiteSpace($name)) {
        return $Node.LocalName
    }

    return "$($Node.LocalName) '$name'"
}

foreach ($node in $xaml.SelectNodes("//*")) {
    foreach ($attributeName in $localizedTextAttributes) {
        $attributeValue = $node.GetAttribute($attributeName)
        if ([string]::IsNullOrWhiteSpace($attributeValue)) {
            continue
        }

        $isBindingOrResource = $attributeValue.TrimStart().StartsWith("{")
        $isAllowedBrandTitle = $node.LocalName -eq "TextBlock" `
            -and $attributeName -eq "Text" `
            -and $attributeValue -eq "Waller"
        if (-not $isBindingOrResource -and -not $isAllowedBrandTitle -and $attributeValue -match "[A-Za-z]") {
            $hardCodedUserText += "$($attributeName) on $(Get-NodeLabel $node): '$attributeValue'"
        }
    }
}

if ($hardCodedUserText.Count -gt 0) {
    Write-Host "Hard-coded user-visible XAML text found; bind to LocalizedText instead:" -ForegroundColor Red
    foreach ($userText in $hardCodedUserText) {
        Write-Host " - $userText" -ForegroundColor Red
    }

    exit 1
}

Write-Host "XAML localization lint passed."
