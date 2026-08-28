$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projects = @(
    (Join-Path $repoRoot "native\Waller.Native.App\Waller.Native.App.csproj"),
    (Join-Path $repoRoot "native\Waller.Native.Tests\Waller.Native.Tests.csproj")
)

foreach ($project in $projects) {
    Write-Host "==> Outdated packages: $project" -ForegroundColor Cyan
    dotnet list $project package --outdated
    if ($LASTEXITCODE -ne 0) {
        throw "Dependency check failed for $project."
    }
}
