# Maintenance changelog

## 2026-08-28

- Added `AGENTS.md` as the canonical agent file and pointed Copilot catalogs
  at it. Reordered VS Code tasks to Run, Verify, Release, Package, deps, audit.
- Bumped `Microsoft.Windows.SDK.BuildTools` to 10.0.28000.2705 and
  `Microsoft.Windows.SDK.BuildTools.WinApp` to 0.6.1. No vulnerable packages
  were reported.
- Routed Store packaging scripts through `Read-WallerPackageManifest` so the
  package-script guards pass.
- Ignored `.scratch/` and `.local/` as local operator storage. Untracked
  `.scratch/planning/.gitignore` so the public tree does not carry scratch.
- Archived completed architecture tickets and the closed 2026-08-12
  modernization note into local `.scratch/archive/` batches. Removed empty
  scratch directories. The local .NET SDK toolchain remains regenerable.

## 2026-08-15

- Updated Windows App SDK to 2.4.0, BuildTools.WinApp to 0.6.0, Microsoft.NET.Test.Sdk to 18.9.0, and the xUnit Visual Studio runner to 4.0.0. Freshness and vulnerability checks use an isolated NuGet cache after the shared machine cache exposed an incomplete package.
- Made the product English-only: legacy `es` settings migrate to English, the Spanish catalog and language selector were removed, and the Settings surface now contains only theme and cache controls.
- Repaired the root `Run` task by passing the WinUI project explicitly and using a self-contained interactive build.
- Updated CI and release Actions to current SHA-pinned releases, added GitHub Funding and a SHA-pinned Pages deployment.
- Added an adaptive Shieldcn README header, five badges, sponsor links, and a 2×2 tour captured from an isolated WinUI proof package with fictional monitors and original Imagegen example art.
- Added a matte English project site and verified it at desktop and mobile sizes with no P0/P1 or runtime failures.
- Final native gate passed 21 static guards, 517 tests, Debug packaged build, and x64 Release build with no warnings or errors.

## 2026-08-12

- Updated active native package references to current NuGet releases:
  Windows SDK BuildTools 10.0.28000.2526, Windows App SDK 2.3.1,
  BuildTools.WinApp 0.5.0, Coverlet 10.0.1, Microsoft.NET.Test.Sdk 18.8.1,
  and xUnit Visual Studio adapter 3.1.5.
- Added `scripts/Check-Dependencies.ps1` and
  `scripts/Audit-Dependencies.ps1` so dependency freshness and security are
  repeatable without introducing a JavaScript package manager.
- Updated `.vscode/tasks.json` with short emoji labels for verify, run, release,
  package, dependency freshness, and audit.
- Refreshed README/docs indexes and added dependency, quality, and maintenance
  reports with upstream changelog links.
- Preserved the native architecture and all `.scratch` evidence; no unrelated
  dirty work was reverted or deleted.

## Follow-up

- Rerun packaged UI Automation and Apply smoke on the target Windows desktop
  after future native dependency changes.
- Keep Store signing, clean-machine, and ARM64 qualification separate from the
  local verification gate.
