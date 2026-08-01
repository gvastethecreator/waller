param(
    [string]$NativePath = "."
)

$ErrorActionPreference = "Stop"

$nativeRoot = if ([System.IO.Path]::IsPathRooted($NativePath)) {
    $NativePath
}
else {
    Join-Path (Split-Path -Parent $PSScriptRoot) $NativePath
}

if (-not (Test-Path -LiteralPath $nativeRoot)) {
    throw "Native path not found: $nativeRoot"
}

$nativeRoot = (Resolve-Path -LiteralPath $nativeRoot).Path
$gitIgnorePath = Join-Path $nativeRoot ".gitignore"
$packagingDocPath = Join-Path $nativeRoot "docs\PACKAGING.md"
$allowedSigningRoot = Join-Path $nativeRoot "artifacts\signing"

if (-not (Test-Path -LiteralPath $gitIgnorePath)) {
    throw "Native .gitignore not found: $gitIgnorePath"
}

if (-not (Test-Path -LiteralPath $packagingDocPath)) {
    throw "Packaging doc not found: $packagingDocPath"
}

$gitIgnore = Get-Content -LiteralPath $gitIgnorePath -Raw
$missingIgnoreRules = @()
foreach ($rule in @("artifacts/", "*.pfx", "*.cer")) {
    if ($gitIgnore -notmatch [regex]::Escape($rule)) {
        $missingIgnoreRules += $rule
    }
}

$unexpectedSigningArtifacts = @()
foreach ($artifact in Get-ChildItem -LiteralPath $nativeRoot -Recurse -File |
    Where-Object { $_.Extension -in @(".pfx", ".cer") }) {
    $artifactPath = $artifact.FullName
    if (-not $artifactPath.StartsWith($allowedSigningRoot, [StringComparison]::OrdinalIgnoreCase)) {
        $unexpectedSigningArtifacts += $artifactPath.Substring($nativeRoot.Length).TrimStart("\")
    }
}

$packagingDoc = Get-Content -LiteralPath $packagingDocPath -Raw
$missingDocTerms = @()
foreach ($term in @(
    "Development signing",
    "Release signing",
    "production certificate decision",
    "timestamping",
    "Signed production distribution still needs")) {
    if ($packagingDoc -notmatch [regex]::Escape($term)) {
        $missingDocTerms += $term
    }
}

if ($missingIgnoreRules.Count -gt 0) {
    Write-Host "Signing artifact ignore rules missing from native .gitignore:" -ForegroundColor Red
    foreach ($rule in $missingIgnoreRules) {
        Write-Host " - $rule" -ForegroundColor Red
    }
}

if ($unexpectedSigningArtifacts.Count -gt 0) {
    Write-Host "Signing artifacts found outside artifacts\signing; keep cert material out of source/docs/scripts:" -ForegroundColor Red
    foreach ($artifact in $unexpectedSigningArtifacts) {
        Write-Host " - $artifact" -ForegroundColor Red
    }
}

if ($missingDocTerms.Count -gt 0) {
    Write-Host "Packaging doc missing signing strategy terms:" -ForegroundColor Red
    foreach ($term in $missingDocTerms) {
        Write-Host " - $term" -ForegroundColor Red
    }
}

if ($missingIgnoreRules.Count -gt 0 -or
    $unexpectedSigningArtifacts.Count -gt 0 -or
    $missingDocTerms.Count -gt 0) {
    exit 1
}

Write-Host "Signing policy guard passed."
