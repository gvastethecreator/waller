# Waller Native Testing

Testing strategy starts in Core and grows outward.

Repo instruction: run checks at the end of a task round, not after every small
edit.

## Current Commands

From `native/`:

Full local verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1
```

Runs XAML accessibility lint, XAML localization lint, WinUI code guards, JSON
code guards, solution build, tests, and packaged launch smoke. The smoke step
includes the packaged build. Any non-zero lint/build/test/smoke command exit
code fails the verification run.

Full local verification without NuGet audit warnings:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -DisableNuGetAudit
```

Runs the same build, tests, and packaged launch smoke steps. The audit override
is also passed through the nested smoke-launch build.

Full local verification without launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Runs XAML accessibility lint, XAML localization lint, WinUI code guards, JSON
code guards, solution build, packaged build without launch, and tests.

Restricted-network/offline version without NuGet audit warnings:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit
```

Verification including Release build:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -Release
```

Fast Release gate without launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Release
```

Fast package gate without launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package
```

Restricted-network package gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit
```

This writes a signed development MSIX under `artifacts\packages\` and inspects
manifest identity/signature. If restricted sandbox networking blocks Release
restore with `NU1301`, rerun the same command outside the restricted sandbox.

Latest no-smoke local result:

- 2026-06-08: Pass after Manage Presets mutation prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Preset save/settings save prefactors.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after selected-Preset loader prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after monitor-assignment update prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after monitor-row selection prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after rendered-cache cleanup prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after main Preset menu refresh prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after apply session-surface refresh prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Preset dropdown stale-load guard.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Preset dropdown load-failure handling.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after monitor-row accessibility name updates.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after row-template accessibility lint hardening.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after startup initialization failure handling.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, solution build,
  packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after adding WinUI code guards to `Verify.ps1`.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Manage Presets delete prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Manage Presets delete replacement-session encapsulation.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after localized-surface refresh prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after top-modal close dispatch prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after monitor source-selection prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Preset save-completion prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Settings save-request prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after disconnected-monitor edit result prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after Apply exception UI-state prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after hard-coded status/progress code guard.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after raw enum fallback cleanup and guard.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- 2026-06-08: Package gate passed outside the restricted sandbox.
- Command: `scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged debug build, tests, Release build, signed dev MSIX
  creation, and MSIX inspection.
- Tests: Passed 138 / Failed 0 / Skipped 0.
- Output: `artifacts\packages\Waller-dev-x64.msix`, size `78,682,578` bytes.
- Trust: dev cert still not trusted
  (`3403C36B7E93446F269873593B379EC2419D5F17`).
- 2026-06-08: Pass after source-path file-type policy.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 139 / Failed 0 / Skipped 0.
- 2026-06-08: Pass after path validation result prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 140 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Apply skipped-result prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 141 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after picker selection validation.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 141 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after renderer placement dimension guard.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 142 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Windows apply order prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after local JSON read helper prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after JSON code guardrail.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after PresetStore directory helper prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after MainPage text-presenter grouping prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after collapsing MainPage presenter fields.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Settings save request/store boundary prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after making Settings draft save internal to store.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after encapsulating Settings save request draft access.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 143 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after monitor-key set factory prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 144 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Preset duplicate factory prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 145 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Preset rename factory prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 146 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Preset save normalization policy prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 147 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after rendered-directory helper prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 147 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Apply result projection prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 148 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Apply cancellation skipped-count helper.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Settings save-result UI projection prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Preset result success-helper prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after managed Preset delete-result helper.
- Command: `scripts\Verify.ps1 -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, tests, and packaged launch smoke.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Smoke: `SMOKE LAUNCH PASSED`.
- Build warnings: 0.
- 2026-06-08: Pass after Manage Presets command-input prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after Preset required-name status prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after monitor assignment result-status prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after selected-Preset load-result projection prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Pass after selected-Preset stale-list refresh policy prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged build, and tests.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Build warnings: 0.
- 2026-06-08: Refreshed development MSIX package gate.
- Command: `scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit`.
- Covered XAML accessibility lint, XAML localization lint, WinUI code guards,
  JSON code guards, solution build, packaged debug build, tests, Release build,
  signed dev MSIX creation, and MSIX inspection.
- Tests: Passed 149 / Failed 0 / Skipped 0.
- Output: `artifacts\packages\Waller-dev-x64.msix`, size `78,687,150` bytes.
- Trust: dev cert still not trusted
  (`3403C36B7E93446F269873593B379EC2419D5F17`).
- Build warnings: 0.
- 2026-06-08: Package asset/identity lint added to `Verify.ps1`.
- Command: `scripts\TestPackageAssets.ps1`.
- Covered manifest display/publisher identity, VisualElements display/description,
  manifest asset references, project Content includes, and MainWindow icon usage.
- Result: passed.
- 2026-06-08: Package version/identity guard added to package lint.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered: non-empty non-template package identity name and MSIX four-part
  version range validation through shared `PackageManifest.ps1` helpers used by
  package lint, version edit, package inspect, and registration preflight
  scripts.
- Result: passed with 149 tests, 0 failures, 0 skipped, 0 warnings.
- 2026-06-08: Uninstall preflight now also uses shared package manifest
  helpers.
- Command: `scripts\UninstallDevPackage.ps1`.
- Result: found current registered debug package and exited before removal, as
  expected without `-Uninstall`.
- 2026-06-08: Package script guard added to `Verify.ps1`.
- Command: `scripts\TestPackageScriptGuards.ps1`.
- Covered: direct package manifest reads outside `PackageManifest.ps1` and MSIX
  package inspection are blocked.
- Result: passed.
- 2026-06-08: Install preflight now reads registration identity from the MSIX
  package manifest.
- Command: `scripts\InstallDevMsix.ps1`.
- Result: inspected `artifacts\packages\Waller-dev-x64.msix`, read package name
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`, found existing current-user debug
  registration, then blocked before cert trust/install as expected.
- 2026-06-08: Uninstall preflight can read identity from a specific MSIX
  package.
- Command: `scripts\UninstallDevPackage.ps1 -PackagePath .\artifacts\packages\Waller-dev-x64.msix`.
- Result: read package name from the MSIX, found existing current-user debug
  registration, and exited before removal as expected without `-Uninstall`.
- 2026-06-08: Registration preflight can read identity from a specific MSIX
  package.
- Command: `scripts\TestDevPackageRegistration.ps1 -PackagePath .\artifacts\packages\Waller-dev-x64.msix`.
- Result: read package name from the MSIX, found existing current-user debug
  registration, and exited with the expected conflict code.
- 2026-06-08: Package identity resolution centralized in
  `Get-WallerPackageIdentity`.
- Command: `scripts\TestDevPackageRegistration.ps1 -PackagePath .\artifacts\packages\Waller-dev-x64.msix`;
  `scripts\UninstallDevPackage.ps1 -PackagePath .\artifacts\packages\Waller-dev-x64.msix`;
  `scripts\InstallDevMsix.ps1`.
- Result: all three resolved the same MSIX package name and stopped at the
  expected existing-registration preflight without uninstalling/installing.
- 2026-06-08: Packaged launch smoke refreshed.
- Command: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit`.
- Result: `SMOKE LAUNCH PASSED`; `winapp` returned AUMID
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`, process
  `Waller.Native.App`, window title `Waller`, `Responding=True`, then cleanup
  closed the launched process.
- 2026-06-08: Current development MSIX package gate refreshed.
- Command: `scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit`.
- Covered: lints, solution build, packaged debug build, tests, Release build,
  signed dev MSIX creation, and MSIX inspection.
- Result: passed with 151 tests, 0 failures, 0 skipped. Output
  `artifacts\packages\Waller-dev-x64.msix`, size `78,687,092` bytes, identity
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`, signature
  `UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17`.
- 2026-06-08: Dev certificate trust preflight prints absolute install command.
- Command: `scripts\TestDevCertificateTrust.ps1`.
- Result: certificate still not trusted, as expected; output included
  `C:\Users\cristian\.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.3.2\tools\win-x64\winapp.exe cert install D:\DEV\waller\native\artifacts\signing\devcert.pfx`.
- Command: `scripts\PrepareDevCertificate.ps1`.
- Result: existing dev cert inspected successfully and output used absolute PFX
  path in the same elevated trust command.
- Command: `scripts\InstallDevMsix.ps1 -SkipRegistrationCheck`.
- Result: MSIX inspection passed, then install preflight blocked on untrusted dev
  certificate and printed the same absolute elevated trust command.
- 2026-06-08: Full local verify gate refreshed.
- Command: `scripts\Verify.ps1 -DisableNuGetAudit`.
- Covered: lints, solution build, tests, packaged debug build, launch smoke,
  process/window verification, and cleanup.
- Result: passed with 151 tests, 0 failures, 0 skipped; smoke returned AUMID
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`, process
  `Waller.Native.App`, title `Waller`, `Responding=True`.
- 2026-06-08: Apply target-plan no-target prefactor.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered: empty ready-key plans select no monitors, and matching plans reject
  null predicates.
- Result: passed with 153 tests, 0 failures, 0 skipped, 0 warnings.
- 2026-06-08: Apply preflight-result factories added.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered: no-target preflight results use empty key sets, and factory-created
  ready/skipped key sets remain case-insensitive.
- Result: passed with 155 tests, 0 failures, 0 skipped, 0 warnings.
- 2026-06-08: Apply cancellation projection moved into `ApplyRunTracker`.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered: partial cancellation result/exception creation from current monitor
  state.
- Result: passed with 150 tests, 0 failures, 0 skipped, 0 warnings.
- 2026-06-08: Apply step-result projection moved into `MonitorApplyStepResult`
  and `ApplyRunTracker.Record`.
- Command: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit`.
- Covered: tracker counter updates from step-result success/failure outcomes.
- Result: passed with 151 tests, 0 failures, 0 skipped, 0 warnings.
- 2026-06-08: Install preflight now checks current-user package registration
  before cert trust/install.
- Command: `scripts\InstallDevMsix.ps1`.
- Result: blocked before cert trust because current-user package registration
  already exists for `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`.
- Registered package:
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_1.0.0.0_x64__yq0fg95n1tr90`.
- Registered location:
  `Waller.Native.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AppX`.
- 2026-06-08: Install preflight gained optional all-user registration check.
- Command: `scripts\InstallDevMsix.ps1 -AllUsersRegistrationCheck`.
- Result: all-user check reported `Acceso denegado`, current-user registration
  was found, then install preflight blocked before cert trust/install.

```powershell
dotnet build .\Waller.Native.slnx
```

```powershell
dotnet test .\Waller.Native.Tests\Waller.Native.Tests.csproj
```

XAML accessibility lint only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestXamlAccessibility.ps1
```

This fails when supported interactive controls in `MainPage.xaml` (`Button`,
`ComboBox`, `ColorPicker`, `ListView`, `NumberBox`, `TextBox`) lack stable
`AutomationProperties.AutomationId` values, when AutomationId values are not
simple stable tokens, when AutomationIds are duplicated in the same XAML scope,
when a TwoWay TextBox `x:Bind` does not update on `PropertyChanged`, or when
monitor/disconnected-monitor row DataTemplate roots with row action buttons lack
`AutomationProperties.Name`. It also blocks hard-coded
`CornerRadius`/`Background` color literals instead of theme resources.

XAML localization lint only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestXamlLocalization.ps1
```

JSON code guards only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestJsonCodeGuards.ps1
```

This fails when app/Core code calls `JsonSerializer.Serialize*` or
`JsonSerializer.Deserialize*` directly outside `LocalJsonFile`, keeping local
data persistence on source-generated metadata.

This blocks new hard-coded user-visible `Text`, `Content`, `Header`,
`Message`, tooltip, and automation name strings; bind new visible copy through
`LocalizedText` instead. The literal `Waller` title is allowed as the product
name.

Packaged WinUI build without launch:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj -SkipRun
```

Packaged WinUI build and launch:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj
```

Packaged launch smoke with automatic process verification and cleanup:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SmokeLaunch.ps1
```

Restricted-network/offline smoke variant:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SmokeLaunch.ps1 -DisableNuGetAudit
```

Release build without signing or launch:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\BuildRelease.ps1
```

Prepare a local development signing certificate without installing/trusting it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\PrepareDevCertificate.ps1
```

Build a signed development MSIX without installing it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\PackageDevMsix.ps1
```

Inspect an existing development MSIX:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InspectDevMsix.ps1
```

Read current package identity/version:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SetPackageVersion.ps1
```

Change package version, then run a gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SetPackageVersion.ps1 -Version 1.0.1.0
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Check whether the local development certificate is trusted:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevCertificateTrust.ps1
```

Run MSIX install preflight without installing:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1
```

Install after certificate trust is ready:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1 -Install
```

Check whether the development package is installed without removing it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1
```

Check registration conflicts across users when launch reports `0x80073D19`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 -AllUsers
```

Expected exit codes:

- `0`: no conflict found in checked scope.
- `2`: current-user package registration found.
- `3`: current user is clean, but all-user check needs elevation.

Legacy uninstall preflight without removing it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1
```

Remove the development package explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1 -Uninstall
```

## Current Tests

Current file:

```text
Waller.Native.Tests\CoreArchitectureTests.cs
```

Coverage:

- sample monitor detector creates Active Session
- empty monitor detector creates an empty Active Session for production fallback
- assignment updates mark monitor/session dirty
- unchanged assignment updates do not mark monitor/session dirty
- missing image preflight marks all affected monitors as Apply errors
- single-monitor missing image preflight marks only the target monitor
- missing image preflight reports ready monitor keys separately from skipped
  monitor keys
- apply target planning selects monitor and preflight ready-key targets
  case-insensitively
- preset exact key matching
- preset fallback matching by resolution and close position when monitor keys
  drift
- preset fallback matching chooses the closest candidate when multiple saved
  monitors have compatible resolution/position
- duplicate preset assignments for the same monitor key do not crash matching
- duplicate preset assignments for the same monitor key are ignored
  case-insensitively
- missing Preset assignments are preserved
- Preset JSON roundtrip
- Preset and Settings JSON reads use shared source-generated local JSON helper
- Preset save normalization goes through `PresetFilePolicy`
- Preset listing uses stable case-insensitive name ordering
- Preset name policy formats default names, trims names, rejects blanks, and
  derives duplicate names consistently
- Preset rename construction goes through `PresetFactory`
- Preset duplicate construction goes through `PresetFactory`
- SolidColor hex policy normalizes optional `#`, lowercases, parses RGB bytes,
  and rejects invalid values
- Preset JSON save keeps the existing file if atomic replacement is blocked
- Preset save normalizes duplicate assignments for the same monitor key
- Preset matching normalizes out-of-range placement offsets before applying
  assignments to Active Session state
- Preset creation from Active Session normalizes duplicate current/missing
  assignments case-insensitively
- Active Session creation and assignment editing normalize out-of-range
  placement offsets before storing desired assignments
- Preset rename/delete JSON operations
- Preset duplicate JSON operation
- App local data write failures use the shared UI guard for friendly Preset and
  Settings status
- Recoverable local filesystem error classification is shared by Core and App
  policies
- corrupt, parseable-invalid, unsupported-schema, or locked Preset JSON files
  are skipped instead of blocking startup/listing or creating blank menu entries
- Preset JSON with invalid assignment entries is skipped before matching
- Preset JSON with invalid source payloads, such as relative image paths or bad
  color values, is skipped before render/apply
- Preset JSON with invalid saved monitor identity payloads, such as blank keys
  or non-positive dimensions, is skipped before matching
- Preset JSON with missing or regressing timestamps is normalized on load
- Preset creation from Active Session with missing assignments
- User settings JSON roundtrip
- User settings JSON save keeps the existing file if atomic replacement is
  blocked
- corrupt or locked User settings JSON falls back to defaults
- unsupported User settings values are normalized
- saved User settings are normalized before writing, including language casing
  and incomplete window position
- supported language codes normalize through the shared `AppLanguages` policy
- supported language culture lookup falls back through `AppLanguages`
- User settings policy centralizes invalid theme fallback, language fallback,
  minimum window size, and incomplete window position cleanup
- window placement is updated through a complete size/position helper
- startup loads saved settings before first session status text
- unexpected startup initialization failures are caught by the page and surfaced
  as localized shell status text instead of escaping the `Loaded` handler
- Rendered cache clear
- Rendered cache clear removes final PNG files and internal temp files while
  keeping unrelated files, including unrelated `.tmp` files
- Rendered cache clear reports a failure when the rendered cache path is blocked
  by a file instead of a directory
- Rendered cache clear reports recoverable enumeration failures instead of
  crashing Settings
- Rendered cache file names sanitize monitor keys, cap long names, and avoid
  sanitized-name collisions with a hash
- Shared atomic file writer writes final output only after the callback
  completes and preserves existing output when the callback fails
- Rendered PNG writing keeps existing final output if an atomic write is
  cancelled
- Monitor topology layout scales negative-coordinate monitor arrangements
  deterministically
- Empty wallpaper path mapping
- Wallpaper path mapping to Image source
- Desktop wallpaper applier missing rendered file guard
- Desktop wallpaper COM writer sets wallpaper before global position
- SolidColor PNG render dimensions
- Image Stretch geometry
- Image Tile geometry
- Cover anchor crop behavior
- Contain black-band behavior
- Image placement planning rejects non-positive source/target dimensions
- Apply service success path
- Apply monitor key matching is case-insensitive
- Apply monitor with an unknown monitor key does not render or touch Windows
- Apply service progress callback
- Image source failure before Windows apply
- Apply service maps renderer/Windows failures through stable error codes to
  friendly monitor errors
- Apply service maps unexpected renderer failures through the same friendly
  fallback without calling Windows apply
- Image picker file type policy includes common wallpaper formats
- Apply error codes are stable tokens, not user-facing prose strings
- Apply error classifier preserves known render/applier codes and maps unknown
  failures to a friendly fallback
- Apply ready-source paths mark missing image monitors as skipped/error and do
  not touch Windows for skipped monitors
- Apply monitor ready-source path uses the preflight ready target and does not
  inspect unrelated missing monitors
- Apply service filtered apply path
- Apply service cancellation propagation without false monitor apply-error state
- Cancelled Apply preserves partial success state
- WinUI compile check covers current lightweight localization bindings
- row source/status labels are bound through lightweight localization
- Apply progress status uses `MonitorApplyStatus`, not raw strings

These tests validate the first architecture boundaries.

## Test Pyramid

Preferred order:

1. Core unit tests
2. Core temp-file integration tests
3. Windows adapter smoke tests
4. WinUI UI automation tests

Keep most behavior in Core so tests stay fast and stable.

Do not reference `Waller.Native.App` from the Core unit test project yet.
Referencing the WinUI app can trigger Windows App SDK auto-initialization under
the xUnit process and fail without package identity/runtime registration
(`REGDB_E_CLASSNOTREG`). App behavior is currently verified by compile,
packaged build, and packaged launch smoke until a dedicated UI test harness
exists.

## Fake Adapters

Use fake adapters for Core tests:

```text
FakeMonitorDetector
FakeWallpaperApplier
FakeWallpaperRenderer
```

Current sample detector can be reused, but dedicated fakes will be clearer once
apply/render behavior exists.

## Fixtures

Suggested future fixture layout:

```text
Waller.Native.Tests/
  Fixtures/
    presets/
      valid-single-monitor.json
      valid-dual-monitor.json
      missing-monitor.json
      extra-monitor.json
    monitors/
      single-monitor.json
      dual-monitor-negative-x.json
      mixed-resolution.json
    images/
      checker-64.png
      wide-160x90.png
      tall-90x160.png
```

Keep fixtures small.

## Core Test Areas

### Active Session

Test:

- startup from one monitor
- startup from multiple monitors
- current source copied into desired assignment
- Empty source when current wallpaper is unknown
- status starts clean

### Editing

Test:

- image assignment
- solid color assignment
- Empty assignment
- fit mode change
- anchor change
- invalid color rejected
- dirty state updates
- original session not mutated unexpectedly

### Preset Matching

Test:

- exact monitor key wins
- fallback by resolution/position works and chooses the closest candidate
- missing monitor preserved
- disconnected monitor assignment visible in UI after loading Preset
- disconnected monitor assignment can be removed without applying wallpaper
- disconnected monitor assignment can be reassigned without applying wallpaper
- new monitor keeps current state
- duplicate/conflicting matches behave deterministically
- negative coordinate monitor matches

### Preset Store

Test:

- save JSON
- load JSON
- list Presets
- invalid JSON handled
- missing directory created
- schema version preserved
- rename/duplicate/delete

### Renderer

Test:

- output dimensions equal monitor pixels
- Empty renders black
- SolidColor renders expected color
- Cover crop math
- Contain bands
- Stretch distorts to target
- Center placement
- Tile repeat
- anchor changes crop/placement
- missing source fails before writing partial output

Renderer image tests can use tiny generated images to avoid large binary
fixtures.

### Apply

Test:

- apply monitor success
- apply monitor render failure
- apply monitor Windows failure
- apply all full success
- apply all partial failure
- cancel active Apply
- successful monitor remains applied after later failure
- Save state does not change on Apply

### Settings

Test:

- default settings
- save/load
- theme enum
- language enum
- window placement
- last selected Preset is visual memory only
- lightweight English/Spanish localizer returns expected labels

## Manual Verification

Manual smoke testing is still required because packaged WinUI launch, Windows
monitor identity, and `IDesktopWallpaper` behavior depend on the real user
session.

Latest packaged launch smoke:

- 2026-06-08: `scripts\Verify.ps1 -DisableNuGetAudit` passed lints, solution
  build, and 160 tests, then blocked at packaged launch smoke with package
  registration conflict `0x80073D19`.
- Error:
  `Another user has already installed an unpackaged version of this app. The current user cannot replace this with a packaged version. The conflicting package is 1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4 and it was published by CN=Waller.`
- Follow-up diagnostic:
  `scripts\TestDevPackageRegistration.ps1 -AllUsers` reported current user is
  clean, but all-user package check failed with `Access is denied.` Run that
  diagnostic from an elevated terminal for a conclusive all-user registration
  check before intentional cleanup.
- 2026-06-08: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the
  restricted sandbox after monitor source-selection prefactor.
- AUMID:
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- Process: `22980`.
- Main window title: `Waller`.
- Responding: `True`.
- 2026-06-08: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the
  restricted sandbox after image picker file-type policy updates.
- AUMID:
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- Process: `10304`.
- Main window title: `Waller`.
- Responding: `True`.
- 2026-06-08: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the
  restricted sandbox after App local-data policy prefactor and defensive
  Core/cache hardening.
- AUMID:
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- Process: `15644`.
- Main window title: `Waller`.
- Responding: `True`.
- 2026-06-08: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the
  restricted sandbox after Core hardening.
- AUMID:
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- Process: `15728`.
- Main window title: `Waller`.
- Responding: `True`.
- 2026-06-08: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the
  restricted sandbox after view-model prefactors.
- AUMID:
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- Process: `38596`.
- Main window title: `Waller`.
- Responding: `True`.

Use this checklist after a build passes. Keep notes beside each item:

```text
Pass / Fail / Blocked
Windows version:
Monitor setup:
App package version:
Notes:
```

### Launch Contract

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SmokeLaunch.ps1
```

Verify:

- App opens through `BuildAndRun.ps1`/`winapp`.
- `SmokeLaunch.ps1` parses detached `winapp` JSON.
- `SmokeLaunch.ps1` sees process `Waller.Native.App`.
- `SmokeLaunch.ps1` sees main window title `Waller`.
- `SmokeLaunch.ps1` sees process responding, then closes launched process.
- `SmokeLaunch.ps1` attempts cleanup from `finally`, so failed assertions do
  not normally leave the launched process open.
- Package manifest publisher is `CN=Waller`, not template `CN=AppPublisher`.
- Windows package/app surfaces show user-facing name `Waller`, not template
  `Waller.Native.App`.
- Raw generated `.exe` is not treated as supported launch path.
- If app does not open, first blocker is package identity/runtime/dev-mode, not
  app UI code.
- Closing and reopening keeps window size/position.
- Corrupt, locked, or inaccessible Settings data does not crash startup or
  shutdown window placement restore/save.

Expected result:

- Waller shell appears.
- No blank window.
- No silent launch from Explorer is required for MVP.

Latest local result:

- 2026-06-08: Blocked after monitor-row notification prefactor.
- Command: `scripts\Verify.ps1 -DisableNuGetAudit`.
- Lints, solution build, and 160 tests passed before packaged launch smoke.
- Packaged launch failed during registration with `0x80073D19`: another user
  already has conflicting package `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`
  published by `CN=Waller`.
- Read-only diagnostic command:
  `scripts\TestDevPackageRegistration.ps1 -AllUsers`.
- Diagnostic result: current user package not registered; all-user check needs
  elevation and returned `Access is denied.`
- 2026-06-07: Pass on Windows 10 Pro 2009, build 26200.
- Command: `BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj -Detach`.
- `winapp` returned AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- Process `Waller.Native.App` stayed running with main window title `Waller`
  and `Responding=True`.
- 2026-06-08: Pass after modal/Apply guard updates.
- Command: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit`.
- `winapp` returned AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`
  and process `16820`.
- Process `Waller.Native.App` responded with main window title `Waller`; smoke
  script cleaned it up.
- 2026-06-08: Pass after Core hardening.
- Command: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit`.
- `winapp` returned AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`
  and process `15728`.
- Process `Waller.Native.App` responded with main window title `Waller`; smoke
  script cleaned it up.
- 2026-06-08: Pass after App local-data policy prefactor and defensive
  Core/cache hardening.
- Command: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit`.
- `winapp` returned AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`
  and process `15644`.
- Process `Waller.Native.App` responded with main window title `Waller`; smoke
  script cleaned it up.
- 2026-06-08: Pass after image picker file-type policy updates.
- Command: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit`.
- `winapp` returned AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`
  and process `10304`.
- Process `Waller.Native.App` responded with main window title `Waller`; smoke
  script cleaned it up.
- 2026-06-08: Pass after row-template accessibility lint hardening.
- Command: `scripts\SmokeLaunch.ps1 -DisableNuGetAudit`.
- `winapp` returned AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`
  and process `11628`.
- Process `Waller.Native.App` responded with main window title `Waller` and
  `Responding=True`; smoke script cleaned it up.

### Startup Detection

Verify with current Windows wallpaper already configured:

- Connected monitors appear in list.
- Monitor IDs/bounds are stable across app restart.
- Current image wallpaper path appears as Image source when Windows reports it.
- Unknown/default Windows wallpaper appears as Empty, meaning black output if
  applied.
- Monitor row names include display index plus shortened Windows device id when
  available.
- Startup does not apply or change wallpaper.
- Friendly fallback text appears if Windows monitor detection fails.
- Empty monitor state renders if detector returns no monitors.
- Topology strip is hidden when no monitors are detected.

Expected result:

- Active Session starts from real Windows state when available.
- Fallback/sample state is visible and friendly if real detection fails.

### Monitor Topology

Test setups when hardware is available:

- Single monitor.
- Two monitors side-by-side.
- Secondary monitor left of primary with negative X.
- Secondary monitor above primary with negative Y.
- Mixed resolution monitors.
- Unplug/replug while app is closed, then reopen.

Verify:

- Topology strip is visible above monitor list.
- Tiles preserve relative size.
- Tiles preserve relative position.
- Negative coordinate monitors still fit inside strip.
- Selected monitor highlight matches selected row.
- Long monitor names do not stretch row layout.

Expected result:

- Topology is a compact map, not equal placeholder blocks.

### Main Shell

Verify:

- Header shows app name, Preset dropdown, Save, Save as, Manage, Settings,
  Apply all.
- Header does not include a persistent Preset name editor.
- Refresh command is visible as an icon button and has tooltip/accessibility
  name.
- Header buttons and Preset dropdown expose stable AutomationIds.
- Refresh reloads current Windows monitor/wallpaper state without applying
  wallpaper.
- Refresh disables while Apply is running and does not replace session state
  mid-apply.
- Preset dropdown stays at top of shell.
- Right edit panel updates when selecting monitor row.
- Monitor row Edit button selects that row and updates the edit panel.
- Monitor/work columns scroll when content exceeds height.
- Status/InfoBar text is readable at common window sizes.
- Footer Apply progress and Cancel command do not overlap the status InfoBar.
- No control overlaps at narrow supported window sizes.

Expected result:

- Shell stays minimal and usable. No dashboard-heavy behavior.

### Source Previews

Create or load monitors with these sources:

- Image with existing path.
- Image with missing path.
- SolidColor.
- Empty.

Verify:

- Existing image source shows thumbnail.
- Missing image source shows warning/fallback preview, not broken image.
- SolidColor source shows matching swatch.
- Empty source shows black preview.
- Source summary text matches selected source.
- Changing source updates row preview without restarting app.

Expected result:

- Row preview communicates what Apply will generate.

### Edit Panel

Verify:

- Image source opens native file picker.
- File picker is parented to app window and returns focus to app.
- Image extension filter accepts common wallpaper images.
- If the picker returns an unsupported or invalid path, the app shows friendly
  validation text and does not change the monitor assignment.
- Image path and Choose image controls are visible only for Image source.
- Relative image paths are rejected before they become session assignments.
- Color field is visible only for SolidColor source.
- Empty source hides Image and Color-specific controls.
- Empty and SolidColor sources keep fit, anchor, position, and Reset position
  controls disabled because those placement values only affect Image rendering.
- Selecting Image with no path keeps Image selected and asks for a path instead
  of changing the monitor assignment to Empty.
- Selected image path becomes current monitor source.
- SolidColor accepts valid `#RRGGBB`.
- Native ColorPicker updates the color field and selected monitor source.
- SolidColor swatches update the color field and selected monitor source.
- Invalid color shows friendly localized validation text.
- Empty remains valid.
- Fit changes update selected row summary.
- Anchor changes update selected row summary.
- Fit modes available: Cover, Contain, Stretch, Center, Tile.
- Anchor positions available: 3x3 positions.
- Source, fit, anchor, and position controls show localized labels, not raw enum
  names.
- Position X/Y controls accept -100..100 and persist through monitor selection,
  Preset save/load, and Apply rendering.
- Reset position returns both X/Y controls to `0,0`, marks the selected monitor
  pending only once, and updates the row placement summary.
- Monitor row image thumbnails reflect selected fit and anchor well enough for
  list preview. Large offsets nudge thumbnail alignment toward the relevant crop
  side; final pixel correctness remains covered by renderer tests.
- Editor inputs and commands expose stable AutomationIds.
- Monitor rows and disconnected monitor rows show localized placement labels,
  not raw enum names.
- Editing marks row/session unsaved.
- Selecting another monitor updates editor fields without marking it unsaved.

Expected result:

- Edit panel changes Active Session only. Wallpaper changes only after Apply.

### Preset Save/Load

Verify:

- Save as creates local Preset.
- Save as opens a small modal, focuses the name field, and creates the local
  Preset only after confirmation.
- Blank Save as name shows friendly validation text.
- Dropdown lists local Presets.
- Loading Preset updates Active Session without applying wallpaper.
- Save updates selected Preset.
- Editing after load marks state unsaved.
- Save clears unsaved state.
- If local Preset writing is blocked by permissions or a locked app-data file,
  the app shows friendly status instead of crashing.
- Preset saves use shared temp-file replacement; failed replacement should not
  leave partial JSON behind.
- No manual JSON import/export is exposed in MVP.
- App data stays under `%LOCALAPPDATA%\Waller\presets`.

Expected result:

- Presets are app-managed local state.

### Manage Presets Modal

Verify:

- Manage opens modal over current shell.
- Initial focus moves to Preset list.
- Empty Manage Presets list shows friendly empty text.
- Rename with blank name shows friendly validation text.
- Rename or Duplicate of a missing/corrupt preset refreshes both the Manage
  Presets list and main dropdown, then shows friendly status.
- Rename works.
- Duplicate works.
- Delete asks confirmation.
- Delete confirmation moves focus to Confirm delete.
- Delete confirmation text includes the selected Preset name.
- Manage Presets list, name input, and modal commands expose stable
  AutomationIds.
- While delete confirmation is open, Preset list/name/rename/duplicate/delete
  controls are disabled so the target cannot change behind Confirm delete.
- Confirm delete deletes the captured Preset target, not a later selection.
- Confirmed delete removes Preset from dropdown.
- If local Preset rename, duplicate, or delete cannot write app data, the app
  shows friendly status instead of crashing.
- Cancel/close leaves Preset unchanged.
- Modal action buttons have accessible names/tooltips.
- Keyboard Tab order is predictable.

Expected result:

- Preset management feels native, small, modal, and reversible except confirmed
  delete.

### Settings Modal

Verify:

- Settings icon has tooltip and accessible name.
- Opening Settings focuses Theme.
- Theme values show localized labels, not raw enum values.
- Language values show localized labels, not raw language codes.
- Changing language updates primary labels without applying wallpaper.
- Theme persists after restart.
- Language persists after restart.
- Clear cache removes rendered output files and stale app render temp files, not
  Presets, settings, or unrelated `.tmp` files.
- Clear cache status reports how many rendered PNG files were deleted and
  whether any were skipped or the cache path is blocked.
- Window size/position persists after restart.
- Window placement restore/save tolerates local Settings read/write failures.
- If Settings cannot write app data, the app shows friendly status instead of
  crashing.
- Settings saves use shared temp-file replacement; failed replacement should not
  leave partial JSON behind.
- Last selected Preset is remembered after selecting/loading or saving it,
  without needing to save Settings manually.
- Last selected Preset can be restored as dropdown visual memory after restart.
- Restored last selected Preset does not auto-load or apply over current Windows
  state on startup.
- Session summary marks restored dropdown selection as visual-only until that
  Preset is explicitly loaded.
- Keyboard Tab order is predictable.

Expected result:

- Settings contains preferences only. No hidden wallpaper apply side effect.

### Disconnected Monitor Presets

Use a Preset with an assignment that does not match current hardware.

Verify:

- Loading Preset shows disconnected monitors section.
- Missing assignment source/placement are visible enough to identify.
- Disconnected image assignments with missing files show localized missing
  source text instead of a stale file name.
- Forget removes missing assignment from Active Session.
- Forget marks session unsaved.
- Saving after Forget removes assignment from next saved Preset.
- Reassign copies source/placement to selected current monitor.
- Reassign normalizes copied placement offsets and handles missing/target
  monitor-key casing case-insensitively.
- Monitor-key sets use the shared case-insensitive `MonitorKeys.CreateSet`
  factory.
- Reassign marks session unsaved.
- Reassign does not apply wallpaper.

Expected result:

- User can clean or recover old monitor assignments without hand-editing JSON.

### Apply Pipeline

Prepare at least two monitors when possible:

- One valid Image source.
- One valid SolidColor or Empty source.
- One missing image path case.

Verify:

- Apply monitor renders/applies selected monitor only.
- Apply all renders/applies current monitors sequentially.
- Apply monitor/all with missing images skips those monitors through Core
  preflight and reports skipped count while applying ready monitors.
- Apply all with only missing image sources reports nothing applied plus the
  skipped count, marks each monitor as missing-source error, and creates no
  rendered output.
- Apply with no current/matching targets reports friendly "nothing to apply"
  status.
- Apply buttons disable while Apply is running.
- Apply/Refresh are disabled while Save as, Manage Presets, delete
  confirmation, or Settings modal is open; keyboard accelerators do not bypass
  the modal.
- Header shell commands, Preset dropdown, and Choose image are disabled behind
  an open modal; modal-local Save/Close/Manage/Settings actions remain usable.
- Source, color, placement, and disconnected-monitor assignment controls are
  disabled behind an open modal and their command handlers ignore mutation until
  the modal closes.
- Cancel Apply clears progress and shows localized cancelled status.
- Double-click or repeated Apply command does not start concurrent operations
  and does not mark extra missing-source errors before the current Apply
  finishes.
- Source editing, Preset save/manage controls, disconnected-monitor actions,
  and cache clear are disabled or ignored while Apply is running.
- If Manage Presets is already open before Apply starts, rename, duplicate,
  delete, confirm delete, name edit, and list selection are disabled until Apply
  finishes; closing the modal remains available.
- If Settings is already open before Apply starts, Save settings and Clear cache
  are disabled until Apply finishes; closing the modal remains available.
- Progress indicator names current monitor/status.
- Success status remains visible after Apply.
- Missing image path blocks affected monitor before Windows apply.
- Missing image path does not block already successful monitors.
- Partial failure is visible.
- Row-level Apply errors use friendly localized summaries, not raw file paths
  or exception text.
- Row-level Apply errors come from stable Core error codes, not parsing
  technical error messages.
- Unexpected Apply failure clears progress and shows a friendly localized
  status without raw exception text.
- Save state does not change only because Apply ran.
- Rendered PNG output lands under `%LOCALAPPDATA%\Waller\rendered`.
- Desktop wallpaper apply requests Windows `Fill` position for rendered PNGs.
- Rendered PNG file names remain valid even when Windows monitor keys contain
  path separators or other invalid file-name characters.
- Rendered PNG output uses the same shared temp-file replacement helper as local
  JSON, committing only after a complete temp-file write instead of exposing a
  partial final file during write.

Expected result:

- Windows is touched only after render/preflight succeeds for that target.

### Placement Visual Check

Use one wide image and one tall image.

Verify per monitor:

- Cover fills monitor and crops predictably.
- Contain shows black bands when aspect ratio differs.
- Stretch fills monitor by distortion.
- Center keeps source centered on black background.
- Tile repeats source.
- Anchor changes Cover crop position.
- Anchor changes Contain/Center placement where visible.
- Empty applies black output.
- SolidColor applies exact color output.

Expected result:

- Per-monitor placement comes from prerendered PNG, not global Windows fit.

### Accessibility and Keyboard

Verify without mouse:

- Tab reaches header commands, monitor list, edit controls, modal controls.
- Focus indicator is visible.
- Enter/Space activate focused buttons.
- Escape closes modal where expected.
- Escape closes delete confirmation before closing Manage Presets.
- Escape is not swallowed when no modal is open.
- Ctrl+S saves selected Preset or opens Save as when current setup has no saved
  Preset.
- Ctrl+Shift+S opens Save as.
- Ctrl+M opens Manage Presets.
- Ctrl+R refreshes current Windows state.
- Ctrl+I opens Settings.
- Ctrl+Enter starts Apply all.
- Tooltips/accessibility names exist for icon-only and compact commands.
- Screen reader labels are meaningful for primary commands, monitor row items,
  disconnected-monitor row items, and modal actions.
- Text remains readable in English and Spanish.
- Startup falls back to Current setup and clears remembered selection when the
  last selected Preset was deleted, corrupt, or unsupported.

Expected result:

- Keyboard-only MVP workflow is possible.

### Real Monitor Detector

When implemented, check:

- single monitor
- two monitors side-by-side
- monitor left of primary with negative X
- monitor above primary with negative Y
- mixed resolutions
- unplug/replug

### Apply

When implemented, check:

- Image/Cover
- Image/Contain
- Image/Stretch
- Image/Center
- Image/Tile
- unsupported manual image extension rejects before session mutation
- SolidColor
- Empty black
- Apply one monitor
- Apply all monitors
- missing image path blocks target monitor only
- unknown `ApplyResult.Failure` error code falls back to
  `wallpaper-apply-failed`
- corrupt, unsupported, locked, or unreadable local JSON continues to recover
  through the shared local JSON read fallback

## CI Notes

CI can run Core tests and possibly build Core.

Full packaged WinUI build may require Windows runner with proper SDK/workloads.

Suggested future CI split:

```text
Core tests
WinUI compile check on Windows
manual packaged launch smoke test
```

Do not block early local development on full UI automation.
