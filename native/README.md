# Waller Native

Fresh WinUI 3 / .NET implementation of Waller.

This folder is intentionally separate from the current Tauri app. The goal is
to build the new native Windows app from zero while using the existing Waller
codebase as behavior reference.

## Current Status

Implemented now:

- `Waller.Native.slnx` solution.
- `Waller.Native.App` WinUI packaged app.
- `Waller.Native.Core` Windows-only domain/core project.
- `Waller.Native.Tests` xUnit project.
- Minimal Fluent shell:
  - header with Preset dropdown and commands
  - monitor topology strip
  - monitor list
  - right-side monitor edit panel
  - status bar
- In-memory Active Session flow using sample monitor data.
- Real Windows monitor/current wallpaper detection through `IDesktopWallpaper`.
- Windows detection COM reads stay behind an internal reader adapter so monitor
  mapping can be tested without querying Windows.
- Current monitor display names include display index plus shortened Windows
  device id when available.
- Invalid Windows wallpaper paths fall back to empty/black source instead of
  failing startup detection.
- Empty monitor fallback when Windows detection fails.
- Native WinUI image file picker.
- Image picker extension policy is centralized in Core and includes common
  wallpaper formats such as JPG, PNG, BMP, WebP, GIF, TIFF, HEIC, and HEIF.
- PNG renderer for `Empty` and `SolidColor` sources.
- Image renderer through Windows codecs.
- Placement modes: Cover, Contain, Stretch, Center, Tile.
- Anchor-aware placement for Cover, Contain, and Center.
- Image placement math is isolated from pixel drawing for direct Cover,
  Contain, and Tile coverage.
- Apply monitor / Apply all path wired through render + Windows applier.
- Windows apply COM writes stay behind an internal writer adapter so success and
  failure mapping can be tested without touching the desktop.
- Header Refresh command reloads current Windows monitor/wallpaper state without
  restarting the app.
- Refresh is disabled while Apply runs to avoid replacing session state
  mid-operation.
- Apply and Refresh are disabled while modals are open, including keyboard
  accelerator paths.
- Header shell commands and image picking are disabled behind open modals while
  modal-local actions remain usable.
- Main-surface monitor assignment edits and disconnected-monitor assignment
  actions are disabled behind open modals while modal-local fields/actions stay
  usable.
- Preset dropdown backed by local JSON.
- Save / Save as for local Presets.
- Manage Presets modal with rename, duplicate, and delete confirmation.
- Preset save/load/selection command flow lives in focused
  `MainPageViewModel.Presets.*.cs` partials, while Manage Presets modal flow
  lives in focused `MainPageViewModel.PresetManagement.*.cs` partials.
- Delete confirmation freezes Manage Presets mutation controls so the selected
  target cannot change behind Confirm delete.
- Delete confirmation captures the target Preset and names it in the warning.
- Manage Presets empty state and friendly validation for blank/missing rename
  targets.
- Missing-source warning and apply preflight.
- Settings modal with theme, language, and cache clear.
- Settings command, load, and option-refresh methods live in
  `MainPageViewModel.Settings.cs`, making a future focused Settings view model
  split cheaper.
- Settings app-data actions are disabled/ignored while Apply runs.
- Apply progress indicator.
- Cancel Apply command while Apply is running.
- Apply commands, progress, cancellation, and run-state UI projection live in
  `MainPageViewModel.Apply.cs`, keeping Apply-only orchestration grouped.
- Apply cancellation token lifetime is isolated in a small view-model helper.
- Apply status/progress/result text projection is isolated in a small
  view-model helper.
- Preset save/load/manage status text projection is isolated in a small
  view-model helper.
- Editor/disconnected-monitor status text projection is isolated in a small
  view-model helper.
- Editor source, placement, selection, assignment, option refresh, and
  disconnected-monitor command flow lives in focused
  `MainPageViewModel.Editor.*.cs` partials.
- Shell/session/settings/cache status text projection is isolated in a small
  view-model helper.
- Apply Core preflight skips missing image sources for monitor/all commands,
  marks affected monitor rows with friendly error codes, and still applies
  ready monitors through explicit ready/skipped monitor-key sets.
- Apply target selection is centralized in Core so all/monitor/ready/filtered
  apply paths share monitor-key handling.
- Apply all with no current monitors is covered in Core and returns an empty
  result without rendering or touching Windows.
- Apply ready-source paths short-circuit when preflight finds no ready monitors,
  returning skipped/no-op results without progress events, rendering, or Windows
  apply calls.
- Apply no-op/skipped-only result construction lives on `ApplySessionResult`,
  so Core callers share the same zero-count outcome contract.
- Apply result counts are validated as non-negative in Core, preventing invalid
  succeeded/failed/skipped totals from reaching UI copy, skipped-count cloning,
  or cancellation state.
- Apply progress counts are validated in Core: completed/total cannot be
  negative, and completed cannot exceed total.
- Apply service keeps per-monitor render/apply failure mapping in a focused
  internal step, leaving the main apply loop to coordinate progress and cancel.
- Apply result/progress contracts live in focused Core files instead of inside
  the service implementation.
- Footer status and Apply progress controls use separate layout columns to
  avoid overlap.
- Lightweight English/Spanish UI text binding.
- English/Spanish copy is isolated in `LocalizedText.Catalog.cs`, while
  formatting/domain projection stays in `LocalizedText.cs`.
- Apply-specific localized result/progress/error text lives in
  `LocalizedText.Apply.cs`.
- Editor, monitor-row, and shell localized projections live in
  `LocalizedText.Editor.cs`, `LocalizedText.Monitor.cs`, and
  `LocalizedText.Shell.cs`.
- Localized saved/unsaved and missing-source row labels.
- Localized status/progress text for main Preset, Settings, and Apply flows.
- Localized formatted status text uses the selected app language culture, not
  the OS current culture.
- Main view-model dependent-property groups are centralized in
  `ViewModelNotificationGroups` before further modal/editor splits.
- Shell initialization, current-session refresh, row/session refresh, modal
  close dispatch, and notification helpers live in
  `MainPageViewModel.Shell.cs`.
- Main-page derived UI projections live in focused
  `MainPageViewModel.Surface.*.cs` partials for editor, modals, monitor
  workspace, Presets, Settings, and shell.
- Main-page observable collections and `[ObservableProperty]` state live in
  focused `MainPageViewModel.State.*.cs` partials for Apply, editor, modals,
  monitor workspace, Presets, and Settings.
- Source-generated property-change hooks live in focused
  `MainPageViewModel.Changes.*.cs` partials for Apply, editor, modals, Presets,
  and Settings.
- Editor source picking/color commands live in
  `MainPageViewModel.Editor.Source.cs`; placement reset/offset helpers,
  monitor selection/hydration, assignment writes, option selection, and
  disconnected-monitor edits live in focused `MainPageViewModel.Editor.*.cs`
  partials.
- Manage Presets modal commands live in focused
  `MainPageViewModel.PresetManagement.*.cs` partials, separate from save/load
  Preset flow.
- First empty-monitor state and icon-only Settings tooltip/accessibility label.
- Disconnected monitors section for Preset assignments that do not match
  current hardware.
- Preset fallback matching handles monitor-key drift by choosing the closest
  same-resolution/near-position assignment.
- Forget action for disconnected Preset assignments; Save keeps the cleanup.
- Reassign action for disconnected Preset assignments to the selected current
  monitor, with normalized placement and case-insensitive monitor-key handling.
- Scrollable monitor/work editor columns so growing content stays reachable.
- Accessible names and tooltips on primary shell commands.
- Explicit localized accessibility names on interactive inputs, pickers, lists,
  and command controls.
- Shared `Controls/IconText.xaml` button content plus XAML resources for action
  icon sizing and icon/text spacing, so native button treatment stays
  consistent.
- Shared `Controls/SourcePreview.xaml` thumbnail rendering for current and
  disconnected monitor rows.
- Shared `Controls/MonitorRow.xaml` visual/action row for current monitors.
- Shared `Controls/MissingMonitorRow.xaml` visual/action row for disconnected
  monitors.
- Shared `Controls/TopologyStrip.xaml` monitor topology strip surface.
- Shared `Controls/MonitorWorkspace.xaml` monitor list, disconnected list,
  empty state, and edit-panel composition.
- Shared `Controls/ShellHeader.xaml` top header/toolbar surface.
- Shared `Controls/SaveAsModal.xaml` Save As modal surface.
- Shared `Controls/ManagePresetsModal.xaml` Manage Presets modal surface.
- Shared `Controls/SettingsModal.xaml` Settings modal surface.
- Shared `Controls/EditPanel.xaml` selected-monitor source/placement editor.
- Shared `Controls/StatusFooter.xaml` status/progress/cancel footer surface.
- Topology tiles and footer status/progress surfaces expose screen-reader
  names/live text, with XAML lint coverage against unnamed topology/status
  surfaces.
- InfoBar status and warning surfaces expose screen-reader names and live
  settings, with XAML lint coverage against silent InfoBar regressions.
- Initial tab order for edit panel, Manage Presets modal, and Settings modal.
- Initial focus moves into Manage Presets and Settings when those modals open.
- Topology strip scales monitors from real bounds instead of showing equal
  placeholder tiles.
- Monitor rows show lightweight source previews for image, color, empty, and
  missing-source states, with preview brush construction isolated from row state.
- Current and disconnected monitor rows share source-summary projection.
- Disconnected monitor rows also show compact source previews, using the same
  preview helpers as current monitor rows.
- Source previews expose accessible names from their source summaries, with
  XAML lint coverage so thumbnail meaning is not visual-only.
- Current and disconnected monitor row refresh notifications share grouped
  property lists, keeping future row preview/status updates consistent.
- User-facing validation/fallback errors avoid raw exception text for common
  edit and monitor-detection paths.
- Apply failures flow through stable Core error codes before localization, so
  row status does not depend on raw exception text.
- Core/App guard blocks new raw `Exception.Message` usage and interpolated
  `ApplyResult.Failure` messages in production code; failures must map to
  stable codes or localized presenters.
- MVP scope guard blocks image editing, Identify, logs, import/export, plugin
  wallpapers, tray behavior, and scheduled wallpaper feature hooks from App/Core
  until the core MVP is proven.
- Settings dropdowns show localized labels for theme and language while
  preserving stored values.
- Settings preference writes preserve window placement while updating theme,
  language, and last selected Preset.
- Edit panel dropdowns show localized source, fit, and anchor labels while
  preserving Core enum values.
- Placement summary copy for monitor rows and disconnected rows uses the same
  localized fit/anchor/offset catalog strings as the edit panel.
- Localized text catalogs are split by language so English/Spanish copy can
  evolve without bloating the selector file. Catalog values use named arguments
  so constructor-order changes stay reviewable.
- Edit panel shows source-specific controls only when relevant.
- Window size and position persistence.
- Window placement restore/save tolerates local Settings failures.
- Last selected Preset visual memory persists during Preset actions and restores
  on startup without auto-loading it over current Windows state.
- Last selected Preset updates preserve Settings preferences and window
  placement.
- Defensive local data loading: corrupt, parseable-invalid, unsupported-schema,
  locked, or inaccessible Preset JSON is skipped and corrupt, locked,
  inaccessible, or unsupported Settings JSON falls back to safe defaults.
- Loaded Preset assignment sources are normalized before use, so invalid image
  paths or colors do not reach render/apply.
- Loaded Preset assignment monitor identities are validated before matching, so
  broken saved geometry does not enter the active session.
- Loaded Preset timestamps are normalized so old or malformed local metadata
  stays consistent.
- Defensive local data writing: blocked Preset/Settings writes show friendly
  localized status instead of crashing the shell.
- App-side local data failures share one recoverable-error policy across Preset,
  Settings, and window-placement paths.
- Core exposes the shared recoverable filesystem-error policy used by App/Core
  local-data paths.
- Preset/Settings saves use shared temp-file replacement so failed writes do not
  leave partial JSON behind.
- Preset matching, creation, and saving share assignment normalization, so stale
  disconnected duplicates and out-of-range offsets do not survive local
  app-managed cycles.
- Active Session creation/editing normalizes placement offsets before storing
  desired assignments.
- Core models for monitors, wallpaper sources, placement, presets, apply state,
  rendered files, and user settings.
- Core services for session creation/editing, preset matching/storage, rendered
  file paths, settings storage, monitor detection, and wallpaper apply seam.
- Rendered output file names sanitize Windows monitor keys and include a short
  hash to avoid invalid paths and sanitized-name collisions.
- Rendered PNG writes use the same shared temp-file replacement helper as local
  JSON, so cancelled/failed writes do not expose partial final output.
- Applying rendered PNGs forces Windows wallpaper position to `Fill`, keeping
  Waller's prerendered fit/anchor/offset result from being reinterpreted by an
  older global Windows placement setting.
- Tests for first Core behavior.
- Detailed manual smoke checklist for packaged launch, real monitor topology,
  source previews, Presets, Settings, disconnected monitors, Apply, placement,
  and keyboard/accessibility behavior.
- Basic keyboard/accessibility polish: primary command tooltips/accessibility
  labels, stable AutomationIds, modal focus entry, Escape modal close, and
  keyboard accelerators for Save, Save as, Manage Presets, Refresh, Settings,
  and Apply all.
- Screen-reader names for monitor rows, disconnected rows, topology tiles,
  footer status/progress, and all interactive controls with automation ids.

Not implemented yet:

- Manual keyboard/accessibility smoke pass on real packaged app.
- Formal `.resw` localization resources, if the lightweight localizer becomes
  too limited.

The shell is a foundation, not product-complete MVP.

## Product Shape

Waller Native is:

- native Windows only
- WinUI 3 / Fluent
- C#/.NET first
- local app, no account model
- built around "Active Session" and local "Presets"

Key product decisions live in:

- `..\docs\prototypes\winui\PRODUCT_DECISIONS.md`
- `..\docs\prototypes\winui\NATIVE_ARCHITECTURE.md`

## Solution Layout

```text
native/
  Waller.Native.slnx
  BuildAndRun.ps1
  Waller.Native.App/
  Waller.Native.Core/
  Waller.Native.Tests/
  docs/
```

### Waller.Native.App

WinUI layer.

Owns:

- XAML views
- ViewModels
- dialogs/modals
- file picker adapter
- window sizing/placement adapter
- app data root and default service composition
- lightweight localization binding

Must not own:

- monitor matching
- preset JSON format
- renderer math
- Windows wallpaper interop
- app data paths beyond UI adapters

### Waller.Native.Core

Domain and Windows-aware app logic.

Owns:

- monitor model
- wallpaper source model
- placement model
- Active Session
- Presets
- settings store
- rendered output store
- monitor detector seam
- wallpaper applier seam

Core is intentionally Windows-only. This keeps the native app simple and avoids
fake cross-platform abstractions for a Windows wallpaper tool.

### Waller.Native.Tests

Core tests.

Target:

- domain behavior
- preset serialization/matching
- apply planning
- renderer math
- settings persistence

Avoid UI tests here. UI automation can be added later as a separate WinUI UI
test slice.

## Build

From `native/`:

Packaging/release workflow details live in `docs\PACKAGING.md`.

Full local verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1
```

Runs XAML accessibility lint, XAML localization lint, WinUI code guards, JSON
code guards, error-text code guards, MVP scope guards, package asset/script
guards, package diagnostic behavior checks, solution build, tests, and packaged
launch smoke. The smoke step includes the packaged build. Any non-zero child
command exit code fails verification.
The XAML accessibility lint checks AutomationIds and accessibility names on
interactive controls and immediate TextBox TwoWay updates across the app XAML
tree, requires button tooltips, requires deterministic `TabIndex` values in
flow modals, blocks duplicate modal `TabIndex` values, requires InfoBar
screen-reader names/live settings, requires accessible source-preview names,
plus basic theme-resource hygiene for corner radii and background colors. The
XAML localization lint blocks new hard-coded visible copy across app XAML,
except the `Waller` brand title.
The WinUI code guard blocks inline async `Loaded` handlers so startup failures
stay mapped through named page handlers with localized status text. It also
blocks inline MainPage `PropertyChanged` handlers so modal focus routing stays
reviewable. It also blocks hard-coded status/progress strings and raw enum
fallback text in C# view models. It also keeps `MainPageViewModel.cs` and
`LocalizedText.cs` from
regaining state/surface/projection responsibilities that now live in focused
partials, keeps placement fit/anchor/offset copy in the localized catalog
instead of `PlacementText.cs`, and keeps concrete English/Spanish catalog
values out of the base selector file. It also blocks unnamed language-catalog
arguments so new localized strings are tied to explicit record fields.
Local app-data root construction stays centralized in `WallerAppDataPaths`; the
WinUI code guard blocks direct `LocalApplicationData` lookups elsewhere.

Without launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Runs XAML accessibility lint, XAML localization lint, WinUI code guards, JSON
code guards, error-text code guards, MVP scope guards, solution build, packaged
build without launch, and tests.

Restricted-network/offline version without NuGet audit warnings:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit
```

Include Release build:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -Release
```

Fast Release gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Release
```

Fast package gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package
```

Individual commands:

```powershell
dotnet build .\Waller.Native.slnx
```

Build WinUI packaged app without launching:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj -SkipRun
```

Run tests:

```powershell
dotnet test .\Waller.Native.Tests\Waller.Native.Tests.csproj
```

## Run

Use `BuildAndRun.ps1`. Do not launch the generated `.exe` directly.

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj
```

Optional detached run:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj -Detach
```

Repeatable packaged launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SmokeLaunch.ps1
```

If `winapp` reports package registration conflict `0x80073D19`, the smoke
script runs current-user and all-users read-only registration diagnostics before
failing.
Package registration lookup stays centralized in `scripts\PackageRegistration.ps1`;
package script guards block raw `Get-AppxPackage` calls elsewhere.
When `-AllUsers` is requested without elevation, registration diagnostics skip
current-user lookup to avoid false current-user package reports; run the
current-user preflight separately without `-AllUsers`.
All-user cleanup is available through
`scripts\UninstallDevPackage.ps1 -AllUsers`, and still requires explicit
`-Uninstall`. If Windows denies all-user inspection, re-run that preflight from
an elevated terminal.

Release build without signing or launch:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\BuildRelease.ps1
```

Release builds currently keep trimming disabled because JSON serialization and
manual COM activation are not trim-safe yet.

Prepare local development certificate without installing/trusting it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\PrepareDevCertificate.ps1
```

Generated certificate files go under `artifacts\signing\` and are ignored by
git. Trusting a certificate still requires an elevated terminal and is a
separate manual step.

Build a signed development MSIX without installing it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\PackageDevMsix.ps1
```

Generated packages go under `artifacts\packages\` and are ignored by git.
Installing the MSIX requires trusting the dev certificate first.
`PackageDevMsix.ps1` runs `InspectDevMsix.ps1` after packaging to verify
manifest identity and signing certificate subject.
`InspectDevMsix.ps1` also reports whether the development certificate is already
trusted. It does not install or trust the certificate.

Read current package identity/version:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SetPackageVersion.ps1
```

Change package version before a package handoff:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SetPackageVersion.ps1 -Version 1.0.1.0
```

Run install preflight without installing:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1
```

Install after trusting the certificate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1 -Install
```

Check installed development package without removing it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1
```

Remove installed development package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1 -Uninstall
```

Reason:

Packaged WinUI apps need package identity. `BuildAndRun.ps1` builds, registers,
and launches through the `winapp` path.

## Runtime Notes

Developer Mode must be enabled:

```text
Settings -> System -> For developers -> Developer Mode
```

`BuildAndRun.ps1` includes a fallback lookup for `winapp.exe` in the NuGet
cache because `winapp` may not be available on PATH.

If the packaged app builds but does not open, first verify:

- `BuildAndRun.ps1` was used.
- Developer Mode is enabled.
- Windows App Runtime version matches the restored `Microsoft.WindowsAppSDK`.
- The app was launched with package identity, not by double-clicking the raw
  build output exe.

## Dependency Notes

Currently used:

- `Microsoft.WindowsAppSDK`
- `Microsoft.Windows.SDK.BuildTools`
- `Microsoft.Windows.SDK.BuildTools.WinApp`
- `CommunityToolkit.Mvvm`
- `xunit`

Planned but not yet added:

- `Microsoft.Windows.CsWin32`

CsWin32 package installation was deferred because package add was blocked during
the first setup pass. Current code uses focused manual `IDesktopWallpaper` COM
interop so development can continue without package restore/network access.
`Waller.Native.Core\Contracts\NativeMethods.txt` remains as a future CsWin32
contract if we decide to switch generation on later.

## App Data Contract

Target app data root:

```text
%LOCALAPPDATA%\Waller
```

Target structure:

```text
%LOCALAPPDATA%\Waller\
  presets\
  rendered\
  settings.json
```

Rules:

- `WallerAppDataPaths` is the App-side source of truth for the root path.
- `WallerAppServices` owns default runtime service wiring for the WinUI app.
- Presets are app-managed local JSON.
- No manual JSON import/export in MVP.
- Rendered PNG files are app-managed cache/output.
- Clear cache deletes rendered PNG files and internal render temp files, keeps
  unrelated files, then reports deleted/skipped counts. A blocked cache path is
  reported as a clear failure instead of a successful empty clear; recoverable
  listing/delete failures become skipped counts instead of crashes.
- Original image paths are stored as full local paths; original files are not
  copied in MVP.

## MVP Source Types

```text
Image       -> original full local image path
SolidColor  -> validated #RRGGBB
Empty       -> black output
```

`Empty` means black wallpaper output. It does not mean skip monitor and does not
mean restore Windows default.

## MVP Placement

Fit modes:

- Cover
- Contain
- Stretch
- Center
- Tile

Anchor:

```text
TopLeft    Top     TopRight
Left       Center  Right
BottomLeft Bottom  BottomRight
```

Placement is controlled by prerendering final per-monitor PNG files, not by
asking Windows for per-monitor fit behavior.

## Next Best Slice

Recommended next slice:

1. Run the manual smoke checklist in `docs\TESTING.md` on real monitor setups.
2. Fix issues found during manual launch/apply/topology/accessibility testing.
3. Decide whether lightweight localization is enough or `.resw` is needed
   after the visible string count stabilizes.

Acceptance:

- Dirty state communicates saved vs unsaved clearly across header and rows.
- Remaining user-visible strings are reviewed for English/Spanish coverage.
- Manual smoke notes identify any real Windows/runtime blockers before more
  feature work.
