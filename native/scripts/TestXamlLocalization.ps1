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

$hardCodedUserText = @()

$localizedTextAttributes = @(
    "AutomationProperties.Name",
    "Content",
    "Header",
    "Message",
    "Text",
    "ToolTipService.ToolTip"
)

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

foreach ($file in $xamlFiles) {
    $relativePath = Get-NativeRelativePath $file.FullName
    $xamlText = Get-Content -LiteralPath $file.FullName -Raw
    $xaml = [xml]$xamlText

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
                $hardCodedUserText += "$($attributeName) on $(Get-NodeLabel $node $relativePath): '$attributeValue'"
            }
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

Write-Host "XAML localization lint passed for $($xamlFiles.Count) file(s)."
