$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projects = @(
    (Join-Path $repoRoot "native\Waller.Native.App\Waller.Native.App.csproj"),
    (Join-Path $repoRoot "native\Waller.Native.Tests\Waller.Native.Tests.csproj")
)

foreach ($project in $projects) {
    Write-Host "==> Vulnerability audit: $project" -ForegroundColor Cyan
    dotnet list $project package --vulnerable --include-transitive
    if ($LASTEXITCODE -ne 0) {
        throw "Dependency audit failed for $project."
    }
}
