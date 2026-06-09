# Waller Native Architecture

This document describes the architecture of the code under `native/`.

The broader product decisions are in the root docs:

- `docs/prototypes/winui/PRODUCT_DECISIONS.md`
- `docs/prototypes/winui/NATIVE_ARCHITECTURE.md`

This file focuses on current implementation boundaries and how to extend them.

## Architecture Goals

Primary goals:

- keep native app independent from current Tauri app
- keep UI thin
- keep domain behavior testable
- keep Windows interop isolated
- support a minimal Fluent UI
- support future per-monitor render/apply pipeline

Non-goals:

- cross-platform Core
- Rust dependency
- portable JSON workflow
- legacy profile compatibility in MVP
- plugin/dynamic wallpaper system

## Dependency Direction

```text
Waller.Native.App
  -> Waller.Native.Core

Waller.Native.Tests
  -> Waller.Native.Core
```

Rules:

- Core must not reference App.
- Core must not depend on XAML or WinUI controls.
- App may reference Core models and services.
- Tests should target Core first.
- UI automation tests should be separate later.

## Current Composition

Current app composition is centralized in `WallerAppServices`:

```csharp
new MainPageViewModel();
```

`WallerAppServices.CreateDefault()` owns default App-side wiring:

```text
WallerAppServices
  -> WallerAppDataPaths
  -> WindowsMonitorDetector
  -> EmptyMonitorDetector
  -> ImageFilePicker
  -> WallpaperRenderer
  -> DesktopWallpaperApplier
  -> PresetStore
  -> UserSettingsStore
  -> RenderedWallpaperStore
  -> MainPageViewModel
```

The app tries Windows first and uses an empty monitor detector as product
fallback when detection fails. Sample monitor data is development/test-only.

Keep dependency injection lightweight. Do not introduce a container until there
is real pressure.

## Domain Model

### ActiveSession

Current editable state.

Rules:

- created from current Windows state on startup
- may be edited without touching Windows
- may be applied without saving
- may be saved without applying

Important fields:

- monitors
- based-on Preset id/name
- missing Preset assignments
- dirty flag

### MonitorSession

One active monitor in the current session.

Contains:

- detected monitor snapshot
- desired assignment
- last applied assignment
- apply status
- apply error
- dirty flag

### MonitorSnapshot

Detected current monitor state.

Contains:

- monitor identity
- display name
- bounds
- current source: per-monitor wallpaper path when present, otherwise Windows
  background color when available, otherwise empty
- current placement when Windows exposes wallpaper position; default placement
  is used when detectors cannot provide it

Bounds must support:

- negative X/Y
- mixed resolutions
- non-primary monitors
- odd topology

Topology scaling for the compact monitor strip goes through
`MonitorTopologyLayout` in Core. Keep monitor bounds normalization there instead
of re-implementing coordinate math in the WinUI view model.
WinUI row collection projection goes through `MonitorRowsProjector`, which maps
`ActiveSession` to current monitor rows, disconnected rows, topology dimensions,
and selection restoration. Keep collection replacement and topology tile
construction out of `MainPageViewModel`.
Monitor selected-row flag updates go through `MonitorRowSelection`, so topology
tile selection, row selection, and editor assignment refresh can move together
when the monitor list is split out.
Monitor and disconnected-monitor row item roots should expose display-name
automation names so screen-reader context survives before individual row action
buttons receive focus.
`TestXamlAccessibility.ps1` enforces that row-template contract for templates
that expose monitor row action buttons.
Monitor/topology/disconnected section visibility goes through
`MonitorRowsSurface`; keep row-surface visibility rules out of
`MainPageViewModel` getters.

### MonitorIdentity

Saved identity plus fallback metadata.

Contains:

- stable monitor key
- device name
- display index
- width/height
- x/y

Matching order:

1. exact monitor key
2. same resolution and close position, choosing the closest fallback candidate
3. missing/disconnected

Monitor-key comparisons must use `MonitorKeys` so matching remains
case-insensitive across Presets, editing, missing-source preflight, and Apply.
Fallback identity comparisons must use `MonitorIdentityMatcher` so the position
tolerance and distance ordering stay consistent.

### WallpaperSource

Supported kinds:

```text
Image
SolidColor
Empty
```

Validation:

- `Image` requires non-empty path.
- `SolidColor` requires `#RRGGBB`.
- `Empty` is valid and renders black.

Solid-color normalization and RGB parsing go through `ColorHexValue`. Keep hex
validation shared between Core render/apply behavior and App color preview
helpers.
Editor quick swatches should come from `ColorSwatchCatalog`, not inline
view-model literals, so future editor palette changes stay local to the editor
projection layer.

Image source path normalization, full-path validation, and supported file-type
validation go through `WallpaperSourcePath`. Image source file existence and
display file-name extraction go through `WallpaperSourceFiles`. UI code should
not hand-roll separate missing-image rules or resolve relative paths against its
working directory.
Use the `TryNormalizeImagePath(..., out ..., out error)` overload when callers
need friendly validation copy; it returns the same `WallpaperSourcePathException`
codes as `NormalizeImagePath` instead of forcing UI code to infer the failure.
Image picker results should be projected through `ImageSelectionDraft`, so
cancel/empty selection, path validation, and selected-file display names follow
the same source path helpers used by row summaries. Invalid picker output should
return localized validation copy and no `MonitorSourceSelection`; do not apply a
source update and then overwrite the error status with "selected".
Editor source changes from file picker and color swatches should go through
`MonitorSourceSelectionFactory`, so source-kind and field updates stay in one
place before a future `MonitorEditViewModel` split.
Native file picker image extensions go through `WallpaperImageFileTypes`, and
manual image paths must pass the same extension policy. Keep picker format
policy near image-source domain rules so the app does not silently diverge from
renderer/Windows-codec expectations.

### WallpaperPlacement

Supported fit modes:

```text
Cover
Contain
Stretch
Center
Tile
```

Supported anchors:

```text
TopLeft, Top, TopRight,
Left, Center, Right,
BottomLeft, Bottom, BottomRight
```

Placement belongs to Core. UI only edits values.

## Core Services

### ActiveSessionFactory

Input:

- `IMonitorDetector`

Output:

- `ActiveSession`

Responsibilities:

- detect monitors
- preserve current Windows source
- set desired assignment to current source and detected placement
- normalize detected placement offsets before storing the desired assignment
- initialize status as clean
- not load Preset automatically
- not apply anything

Windows placement detection is best-effort. `IDesktopWallpaper.GetPosition`
failure must fall back to `WallpaperPlacement.Default` without discarding
monitor/source detection.
Windows background-color detection is also best-effort. `GetBackgroundColor`
failure must keep empty source fallback instead of failing monitor detection.
Windows wallpaper path mapping should use `WallpaperSourcePath.TryNormalizeImagePath`.
Invalid or relative paths from Windows should map to empty source so startup
detection can continue.

### ActiveSessionEditor

Responsibilities:

- update one monitor assignment
- mark monitor dirty
- mark session dirty
- normalize placement offsets before comparing or storing the desired assignment
- reassign disconnected assignments through the same placement normalization and
  case-insensitive monitor-key rules used by regular assignment edits
- expose focused session-edit helpers used by UI commands
- return a new session object

Do not mutate existing session in place. This keeps future undo/testing easier.

Missing-image Apply preflight is shared through `ApplyPreflight`, so UI command
handlers and Core apply orchestration do not duplicate missing-source rules.
`ApplyPreflightResult` carries ready monitor keys and skipped monitor keys; app
and service code should consume those sets instead of rebuilding equivalent
predicates.
Use `HasReadyMonitors` and `HasSkippedMonitors` when callers need boolean flow
decisions, instead of scattering set-count checks.
Use `SkippedCount` when reporting skipped monitors in apply summaries, instead
of reaching into `SkippedMonitorKeys.Count`.
`WallpaperApplyService` converts user intent and preflight output into
`ApplyTargetPlan`; keep monitor-key comparison and target counting there instead
of scattering selection lambdas through the apply loop.

### PresetMatcher

Responsibilities:

- load Preset assignments into current Active Session
- match by monitor key
- match monitor-key drift by closest compatible resolution/position fallback
- normalize assignments before matching so legacy JSON cannot leak duplicate
  monitor keys or out-of-range placement offsets into active session state
- preserve missing assignments
- keep new monitors with their current Windows source
- set dirty state appropriately

The matcher does not save files and does not touch Windows.

`WallpaperPlacement` includes fit, anchor, and optional X/Y percent offsets.
Offsets default to `0,0` so older Preset JSON remains valid. Rendering clamps
offset movement to the safe origin range: Cover does not reveal black bands, and
Contain/Center do not move the image outside the monitor canvas. The WinUI
editor exposes those offsets through compact `NumberBox` controls bound to the
same `OffsetXPercent` and `OffsetYPercent` fields.
Offset bounds are centralized in `WallpaperPlacement.ClampOffset` and
`NormalizeOffsets`; app drafts, active sessions, rendering, and
`PresetAssignments.Normalize` must use those helpers rather than each keeping
separate -100..100 policies.
Monitor-row thumbnails are a lightweight UI preview only: `MonitorSourcePreview`
owns preview brush/image-brush creation, while `PlacementPreview` maps
`WallpaperPlacement` to WinUI `ImageBrush` stretch/alignment. Fit and anchor are
reflected directly; non-zero offsets nudge alignment coarsely toward the side of
the crop the final renderer will show. Final wallpaper pixels still come from
Core rendering. Do not move final placement behavior into the view model.

### PresetStore

Responsibilities:

- list Presets
- load Preset JSON
- save Preset JSON

Lists are sorted by ordinal case-insensitive name with id tie-breaks. Do not use
current-culture sorting for persisted Preset menus; dropdown order should be
stable across language changes.
Preset directory creation should go through `EnsurePresetsDirectory`, and preset
file discovery should use the named JSON search pattern so future local Preset
maintenance operations share one filesystem policy.
Loaded Preset JSON goes through `PresetFilePolicy`: corrupt JSON, parseable but
invalid shape, empty ids/names, missing assignment lists, and unsupported schema
versions are skipped until an explicit migration exists. Do not let invalid
local files create blank Preset menu entries.
Loaded assignment lists should use `PresetAssignments.TryNormalize`, so null
entries or invalid source/placement enum values reject the file explicitly.
Assignment source payloads should normalize through `WallpaperSource.TryNormalize`
when loaded from JSON. Image paths must be full local paths with supported image
extensions and solid colors must parse through `ColorHexValue`; invalid payloads
reject the whole local Preset file until migration/repair exists.
Saved monitor identity payloads must pass `MonitorIdentity.IsValidForPresetAssignment`:
monitor key is required, and width/height must be positive so fallback matching
never consumes broken geometry.
Loaded Preset timestamps are normalized by `PresetFilePolicy`: missing
timestamps fall back to `UnixEpoch`, and `updatedAt` never remains earlier than
`createdAt`.
Saved Preset normalization should also go through `PresetFilePolicy.NormalizeForSave`;
`PresetStore.SaveAsync` should own app-data paths and JSON writes, not
schema/name/assignment/timestamp policy.

Preset default names, validation, and duplicate-name derivation go through
`PresetNames`. WinUI input projection should use `PresetNameInput` before
calling stores/factories. Keep timestamp formatting, trimming/blank checks, and
duplicate suffix rules out of WinUI command handlers. When a command needs
required-name validation, use the `PresetNameInput` overload that also returns
localized status text instead of setting name-required status inside the view
model.

Preset assignment cleanup goes through `PresetAssignments.Normalize`.
`PresetMatcher`, `PresetFactory`, and `PresetStore` must share this policy so
loaded, created, and saved assignments are deduped with the same
case-insensitive monitor-key rules used by matching/edit/apply paths.
Duplicate Preset construction should go through `PresetFactory.Duplicate`;
`PresetStore` should persist the returned Preset rather than owning id/name/time
creation policy.
Rename Preset construction should go through `PresetFactory.Rename`; `PresetStore`
should persist the returned Preset rather than owning name mutation policy.

Planned responsibilities:

- rename
- duplicate
- delete
- schema migration

File shape:

```json
{
  "schemaVersion": 1,
  "id": "guid",
  "name": "Preset name",
  "assignments": []
}
```

### RenderedWallpaperStore

Responsibilities:

- choose rendered output paths
- ensure rendered folder exists
- later: clean cache on explicit user request

Rendered file names must sanitize Windows monitor keys, cap the readable prefix,
and include a short hash. Windows monitor device paths can contain file-name
invalid characters or be very long; the hash keeps distinct monitor keys from
colliding after sanitization.
Rendered output directory creation should go through `EnsureRenderedDirectory`
inside `RenderedWallpaperStore`, so future rendered-output maintenance reuses
one folder policy.

Rendered PNG writes go through `AtomicFileWriter`: write a same-folder temp file,
flush it, then replace the final path after the PNG is complete. If render
writing is cancelled or fails, the previous final file stays intact and temp
output is cleaned up.

Rendered cache cleanup removes final `.png` outputs and internal `.tmp` render
files that match the app's atomic render-write pattern. Other files in the
rendered directory, including unrelated `.tmp` files, are left alone.
If the rendered cache path is blocked by a file instead of a directory, cleanup
must report a failure rather than returning a successful empty result.
Recoverable enumeration/delete failures should increase the failure count rather
than escaping into the Settings command.
Use `RenderedCacheClearResult.HasFailures` when formatting user-facing cache
clear summaries instead of checking raw failure counts in UI text code.
Settings cache-clear commands should go through `RenderedCacheCleanup`, keeping
direct rendered-cache store calls out of modal command handlers.

Root:

```text
%LOCALAPPDATA%\Waller\rendered
```

### IWallpaperRenderer

Planned responsibilities:

- load source image
- render final PNG at monitor resolution
- apply fit/anchor
- use black background for Empty/Contain/Center gaps
- return `RenderedWallpaper`

This interface should not call Windows wallpaper APIs.

`BasicPngWallpaperRenderer` owns render orchestration only. Image placement math
is calculated by `ImagePlacementPlan` and consumed by `ImagePlacementRenderer`;
pixel storage, image decoding, RGB conversion, and PNG writing live in separate
internal helpers so future decoder/writer/placement changes stay isolated.
`ImagePlacementPlan` validates source and target dimensions before fit/anchor
math, so scaling bugs fail with clear parameter errors instead of divide-by-zero
or nonsensical origins.
`PixelBuffer` owns positive dimension and RGB byte-length validation; renderers
should not pass unchecked dimensions or mismatched pixel arrays through to PNG
writing.

### IWallpaperApplier

Responsibilities:

- apply already-rendered PNG to Windows
- return per-monitor result
- not render
- not mutate Presets

Production implementation will wrap Windows APIs.
`DesktopWallpaperApplier` must keep COM calls behind the internal
`IDesktopWallpaperWriter` adapter. The public applier maps file/preflight and
writer failures to stable `ApplyResult` values; the writer owns the raw
`IDesktopWallpaper.SetPosition` and `IDesktopWallpaper.SetWallpaper` calls.
Because Waller renders the requested per-monitor placement into monitor-sized
PNGs before calling Windows, apply should set the global Windows position to
`Fill` for the rendered output instead of letting a previous Windows `Fit`,
`Center`, or `Tile` setting reinterpret the PNG.
The raw COM writer should set the rendered wallpaper path before setting the
global position. If the wallpaper call fails, Waller should not still leave a
global Windows position change behind.

### IMonitorDetector

Responsibilities:

- enumerate monitors
- collect bounds
- read current per-monitor wallpaper when possible
- return stable monitor identity metadata

`SampleMonitorDetector` exists for shell development only.
`WindowsMonitorDetector` must keep raw COM reads behind the internal
`IDesktopWallpaperReader` adapter. The detector maps snapshots into stable
`MonitorSnapshot` values and owns app fallback policy such as empty wallpaper
path -> background source.
Windows detector display names should go through `DesktopMonitorDisplayName` so
rows include display index plus shortened device id while `MonitorIdentity`
keeps the full monitor key for matching/persistence.
`EmptyMonitorDetector` exists for product fallback when Windows detection fails.

## App Layer

### MainPage

Current shell:

```text
Header
Monitor topology strip
Monitor list
Edit panel
InfoBar
```

This is intentionally one screen. No permanent sidebar.
Startup is owned by `MainPage.OnLoaded`, which awaits
`MainPageViewModel.InitializeAsync` and maps unexpected failures to localized
shell status text. Do not attach raw `async` lambdas to `Loaded`; unobserved
startup exceptions can otherwise escape the shell.
`scripts\TestWinUICodeGuards.ps1` enforces this rule before build/test work.

### MainPageViewModel

Responsibilities:

- initialize Active Session
- expose rows
- expose selected monitor
- update edit fields
- call Core editor
- coordinate Core apply, Preset, Settings, render-cache, and Windows services

Preset dropdown projection goes through `PresetMenuLists`. Main Preset dropdown
refresh goes through `PresetMenuRefresh`, and Manage Presets modal refresh goes
through `ManagedPresetList`; those helpers compose store listing, collection
replacement, selection fallback, and selected-Preset visual-memory ids. Keep
those mechanics out of command handlers so future focused Preset view models can
move menu surfaces without rediscovering menu-list rules.
Manage Presets command guards for selected preset id and required names should
go through `ManagedPresetCommandInput`, which composes `ManagedPresetSelection`
and `PresetNameInput`, so rename, duplicate, and delete do not drift in
validation/status behavior.
Manage Presets mutations that can hit stale files or recoverable local-data
failures should return explicit `ManagedPresetMutationResult` values, keeping
exception mapping out of command handlers.
Command handlers should consume successful mutation values through
`ManagedPresetMutationResult.TryGetValue` instead of checking nullable payload
shape directly.
Missing managed Preset handling should refresh both the modal list and the main
Preset dropdown, so stale file-system state disappears from every Preset entry
point after one failed rename, duplicate, or delete attempt.
Delete confirmation target capture goes through `PresetDeleteConfirmation`.
Keep the modal target id/name together so confirmation text and final delete
cannot drift when Manage Presets selection changes behind the modal.
Confirmed delete goes through `ManagedPresetDelete`, which combines the store
delete result with the replacement selection needed when the deleted Preset was
the active session base. Keep that active-Preset decision out of the command
handler.
Command handlers should consume successful delete replacement through
`ManagedPresetDeleteResult.TryGetSuccessfulReplacement`, keeping write-failure
checks with the delete result DTO.

Settings and editor dropdown option replacement/selection goes through
`OptionItems`. Keep option-list mutation and equality rules out of individual
property-change handlers. Use `ReplaceAndSelect` when refreshing a whole
localized option list so replacement and selected-value restoration stay one
operation.
Localized option projection for Settings and editor dropdowns goes through
`LocalizedOptionCatalog`; full localized refreshes should go through
`LocalizedOptionSelections`, so Settings and editor split work can reuse one
option-refresh contract. The main view model should request option lists, not
construct enum/language menu items inline.

Derived command permissions such as `CanStartApply`, `CanEditSession`,
`CanEditMonitorAssignment`, `CanUseShellCommands`, `CanMutateManagedPresets`,
and `CanUseModalActions` should flow through `ShellInteractionState`.
`CanEditPlacement` can add placement-specific checks on top of that shared
state. Top modal ordering for Escape/close behavior should also flow through
`ShellInteractionState.TopModal`, and top-modal close dispatch should go through
`ShellModalClose`, not open-coded modal priority chains. Refresh those derived
properties through the shared command-state
notification helper, not hand-notified one by one from new modal/apply state
changes. Keep main-surface assignment edits behind `CanEditMonitorAssignment`
so source/color/placement/disconnected-monitor commands cannot mutate the active
session while a modal is open. Keep modal-local buttons and fields behind
`CanUseModalActions` so Save as, Settings, and confirmation actions can continue
inside the active modal while Apply is idle.
Apply commands should start and finish cancellation state through the shared
apply-run helpers. Pass the `CancellationToken` into Core services explicitly;
do not reach back into nullable view-model fields from apply lambdas.
Apply command target construction should go through `ApplyRunRequest`, so
`MainPageViewModel` chooses all vs one monitor without embedding
ready-source-service lambdas inline.
`ApplyRunState` owns `CancellationTokenSource` lifetime for the view model; keep
begin/cancel/dispose mechanics there instead of adding CTS fields back to
`MainPageViewModel`.
Repeated dependent-property notifications should use `ViewModelNotificationGroups`,
the shared multi-property notification helper, or a focused semantic helper such
as selected-source warning notification, not open-coded `OnPropertyChanged`
clusters.
Surface-specific dependent notifications such as rows, selected monitor, source
editor visibility, and delete confirmation should use focused
`ViewModelNotificationGroups` entries instead of inline property-name arrays.
Delete confirmation target changes should use the semantic delete-confirmation
notification helper, not direct `DeleteConfirmationMessage` notifications.
Language refresh, modal visibility, modal-open state, and apply-progress
visibility should follow the same grouped-notification path, so future Settings
or modal view-model splits can move one surface without rediscovering dependent
property names.
Session summary refresh should use the semantic summary notification helper,
not direct `OnPropertyChanged(nameof(SessionSummary))` calls.
Shell initialization, current-session refresh, row/session refresh, modal close
dispatch, and notification helpers currently live in
`MainPageViewModel.Shell.cs`. Keep shell-level orchestration there while the
main partial remains focused on state, derived properties, and property-change
hooks.
Source-generated property-change hooks currently live in
`MainPageViewModel.Changes.cs`. Keep those reactive glue methods there so the
main partial stays limited to state and derived properties.

Modal overlay backgrounds should use the app-level `WallerModalOverlayBrush`
resource. Do not repeat hard-coded scrim colors in page XAML.
Corner radii should use WinUI theme resources (`OverlayCornerRadius` for
surfaces/modals, `ControlCornerRadius` for compact controls/previews) rather
than numeric XAML literals.

WinUI `Visibility` projection should go through `VisibilityStates` instead of
hand-writing `Visibility.Visible`/`Visibility.Collapsed` ternaries in view
models.

User-facing Apply summary copy belongs in `LocalizedText`; view-model apply
orchestration should pass the `ApplySessionResult` to the localizer instead of
assembling success/failure/skipped text inline.
`LocalizedText` is split by responsibility: `LocalizedText.Catalog.cs` owns the
English/Spanish string catalog and language selection, while `LocalizedText.cs`
owns formatting and domain/result projection. If the app moves to `.resw`, keep
the current call surface stable and replace only the catalog/provider layer.
Use `ApplySessionResult.HasAppliedOutcome` when deciding whether an
apply-finished summary should mention succeeded/failed monitor counts. Use
`HasAnyOutcome` only when skipped-only results should still count as an Apply
outcome. This keeps all-missing-source attempts from looking like a completed
0-success/0-failure apply.
Use `ApplySessionResult.WithSkipped` when preflight needs to add skipped-target
accounting to a normal render/apply result. Use `ApplyCanceledException.WithSkipped`
for cancellation partial results so service catch sites do not manually rebuild
exception/result pairs.
Apply progress copy should follow the same rule: pass `ApplyProgress` to
`LocalizedText` rather than formatting monitor/status counters inline in apply
orchestration.
`ApplyTextPresenter` is the view-model-side adapter for apply text; keep
preparing/progress/result/cancel/failure projection there instead of calling
multiple `LocalizedText` apply members throughout `MainPageViewModel`.
Apply run completion UI projection goes through `ApplyRunUiState`; keep
success/cancel/failure progress clearing and status selection out of catch
blocks. Exception-to-UI-state mapping also belongs in `ApplyRunUiState`, not in
`MainPageViewModel.RunApplyAsync`.
Apply command methods, progress updates, cancellation, and run-state projection
currently live in `MainPageViewModel.Apply.cs`. Keep Apply-only UI
orchestration there while the code moves toward a focused Apply surface.
When apply returns a replacement `ActiveSession`, present it through the shared
session-surface refresh helper so monitor rows, selected monitor state, and
session summary notifications stay together.
`PresetTextPresenter` is the equivalent adapter for preset save/load/manage
status text; keep preset-specific prompt/result projection there while the
Manage Presets commands still live in `MainPageViewModel`.
Preset save/load/manage commands and helper flow currently live in
`MainPageViewModel.Presets.cs`. Keep Preset-only orchestration there while the
code moves toward a focused Presets surface or view model.
Manage Presets modal commands now live in
`MainPageViewModel.PresetManagement.cs`; keep rename/duplicate/delete modal
state and managed-list refresh there, while `MainPageViewModel.Presets.cs`
stays focused on save/save-as/load/selected-preset session transitions.
Keep public Apply DTOs in focused files (`ApplySessionResult`, `ApplyProgress`,
`ApplyPreflightResult`) so `WallpaperApplyService` and `ApplyPreflight` remain
orchestration, not grab bags of contracts.
Selected-source warnings and rendered-cache clear summaries should also be
formatted by `LocalizedText`; view models should pass domain/result objects
instead of assembling warning/status strings inline.
Edit validation exception messages should follow the same pattern: pass the
`ArgumentException` to `LocalizedText.ValidationMessage` instead of mapping
parameter names or source-path error codes in command handlers.
`MonitorEditTextPresenter` owns editor and disconnected-monitor status text
projection for image selection, missing image paths, validation failures,
pending changes, forget, and reassign actions. Keep those calls there while the
editor still lives inside `MainPageViewModel`.
Editor, source-selection, placement, and disconnected-monitor command flow
currently lives in `MainPageViewModel.Editor.cs`. Keep editor-only orchestration
there while the code moves toward a focused editor surface.
`ShellStatusTextPresenter` owns shell-level status text for current-session
load/refresh, Settings open/save, local-data write failures, and rendered-cache
clear summaries. Keep broad shell/status messages there so a future
`SettingsViewModel` can move without copying localization rules.
Settings command, load, and option-refresh methods currently live in
`MainPageViewModel.Settings.cs`. Keep additional Settings-only orchestration in
that partial until the surface is ready to become a focused `SettingsViewModel`.
`MainPageTextPresenters` is the construction point for MainPage presenter
instances; keep shared `LocalizedText` provider wiring there while command and
surface responsibilities continue splitting out of `MainPageViewModel`.
`MainPageViewModel` should hold that presenter group as one dependency; private
aliases can preserve existing command readability while deeper command extraction
continues.

Monitor/disconnected-row display formatting belongs in focused helpers:
localized resolution, bounds, and status copy goes through `LocalizedText`,
placement fit/anchor/offset copy goes through `PlacementText`, and shared
source-summary projection goes through `MonitorSourceText`. Row view models
should not assemble those strings inline.
Runtime language refresh for row text and the Current setup Preset label goes
through `LocalizedSurfaceRefresh`, so future Settings/localization splits do
not need to rediscover every surface that caches localized display text.

Header Active Session summary formatting also belongs in `LocalizedText`; the
main view model should pass session/preset state, not assemble suffix text
inline.
Active Preset session transitions should go through `ActivePresetSession`:
save, rename-active, delete-active, and Current setup selection must share the
same `BasedOnPreset`, dirty-state, and missing-assignment policy.
Renaming the active Preset should use `ActivePresetSession.RenameActive`, which
returns the updated session plus selected-Preset record/name draft projection.
Preset dropdown selection state should go through `SelectedPresetSessionLoader`
and `SelectedPresetSessionFactory` before mutating `MainPageViewModel`, so
Current setup selection, loaded Preset selection, missing-Preset refresh, and
deleted-active-Preset cleanup share session/draft/visual-memory projection
rules. `SelectedPresetLoadResult` should own loaded selection projection and
status text/list-refresh policy selection, so dropdown load handlers do not
switch on result kind to pick copy, nullable session state, or stale-list cleanup.
Preset dropdown async loads must be versioned by the view model so slower stale
loads cannot overwrite a newer user selection or programmatic menu refresh.
Fire-and-forget Preset dropdown loads must catch unexpected loader failures and
surface localized `PresetTextPresenter` status text; do not leave unobserved
task exceptions behind the combo box.
After a Preset save succeeds, use the shared post-save helper in
`MainPageViewModel` to mark the active session saved, refresh the Preset menu,
persist visual selection memory, and refresh the session surface together.
Save vs Save as selected-record/name-draft projection should go through
`PresetSaveCompletion`, so command handlers do not duplicate which fields are
updated after each save mode.
Preset save and Save as construction should go through `PresetSessionSave`, so
`PresetFactory`, `PresetStore.SaveAsync`, and recoverable write-failure mapping
stay out of shell command handlers.
Save command handlers should consume saved Presets through
`PresetSessionSaveResult.TryGetPreset`, keeping result-shape checks with the
result DTO.

Editor field projection and source/placement reconstruction go through
`MonitorEditDraft`. Keep conversion between selected monitor assignments and
editable fields there so a future `MonitorEditViewModel` split can reuse the
same rules. Applying editor fields to an active session should go through
`MonitorAssignmentUpdate`, which wraps `MonitorEditDraft.ApplyTo` plus missing
image/invalid-value outcomes so command handlers do not rebuild
source/placement conversion or catch edit exceptions before calling
`ActiveSessionEditor`. `MonitorAssignmentUpdateResult` should own updated-session
projection and edit status text selection, keeping `MainPageViewModel` from
checking missing-image/invalid-value flags directly.
Picker and swatch commands should create `MonitorSourceSelection` values and
let the view model apply those values, rather than manually assigning source
kind plus image/color fields in each command.
Disconnected monitor edit actions should go through `DisconnectedMonitorEdit`,
so forget/reassign commands keep row-to-session-key mapping out of
`MainPageViewModel` while the focused editor split is still pending. The helper
returns `DisconnectedMonitorEditResult`, carrying optional replacement session
plus localized status text so missing-target and success projection stay out of
individual command handlers.
Selected-monitor editor surface projection belongs in `MonitorEditorSurface`:
edit-panel visibility, selected monitor display name, source editor visibility,
and selected-source warnings should not be reassembled in `MainPageViewModel`.

Future split:

- `MainViewModel`
- `PresetMenuViewModel`
- `MonitorEditViewModel`
- `ManagePresetsViewModel`
- `SettingsViewModel`

Do not split early unless complexity demands it.

### MonitorRowViewModel

Display wrapper around `MonitorSession`.

Responsibilities:

- monitor name
- resolution
- bounds
- source summary
- placement summary
- apply/save status

Row view models should use their shared multi-property notification helpers for
localized text/session refreshes. Avoid adding new open-coded
`OnPropertyChanged` clusters when placement preview, source preview, or status
dependencies change together.

## Apply Pipeline

Target flow:

```text
User clicks Apply
-> VM asks Core to plan work/preflight
-> renderer creates one PNG per selected monitor
-> applier calls Windows
-> Core updates per-monitor status
-> UI reflects result
```

Apply commands use ready-source service entrypoints:
`WallpaperApplyService.ApplyMonitorReadySourceAsync` and
`WallpaperApplyService.ApplyAllReadySourcesAsync`. Core marks missing image
sources as friendly monitor errors, skips those monitors, applies remaining
ready monitors, and reports the skipped count in `ApplySessionResult`. The WinUI
layer should not build skip predicates for missing sources itself.
`WallpaperApplyService` should also consume `ApplyPreflightResult.ReadyMonitorKeys`
directly, keeping ready-target selection and skipped-target accounting inside
Core.
Selection variants (`all`, one monitor, ready keys, or filtered matching) should
enter the apply loop as `ApplyTargetPlan` so future batch/selected workflows can
reuse one target abstraction.

Partial failure rule:

- successful monitors stay applied
- failed monitors show error
- no automatic rollback
- Active Session remains editable

## Save Pipeline

Target flow:

```text
User clicks Save
-> current Active Session serializes as selected Preset
-> dirty state clears
-> Windows is not touched
```

Save and Apply remain independent.

## Startup Pipeline

Target flow:

```text
Launch
-> detect current monitors
-> read current wallpaper state
-> create Active Session
-> show Current setup
-> do not auto-load Preset
-> do not auto-apply
```

App may remember last selected Preset visually later, but must not auto-load or
auto-apply it.

Current setup detection/fallback orchestration belongs in `CurrentSessionLoader`:
try the primary Windows detector first, treat an empty primary session as
fallback, catch detector failures, then create the fallback session from
`EmptyMonitorDetector`. `MainPageViewModel` should receive the loaded session
and `UsedFallback` flag, not own detector exception policy.

## File System Contract

Root:

```text
%LOCALAPPDATA%\Waller
```

Subfolders/files:

```text
presets\
rendered\
settings.json
```

No user-facing import/export in MVP.

Preset and Settings JSON use `WallerJsonContext` source-generated metadata.
Avoid reflection-based `JsonSerializer` overloads in app persistence paths; they
produce trim warnings in Release builds.

Preset and Settings reads/writes go through `LocalJsonFile`, using
source-generated JSON metadata for both directions. Writes use `AtomicFileWriter`:
serialize to a same-folder temp file, flush, then replace the destination. This
keeps existing local JSON readable if replacement fails because the file is
locked or app data is inaccessible. Reuse `AtomicFileWriter` for new app-managed
local files that must not expose partial output.

`TestJsonCodeGuards.ps1` enforces this boundary by failing direct
`JsonSerializer` calls in Core/App persistence code outside `LocalJsonFile`.

Supported language codes go through `AppLanguages`. Settings normalization and
WinUI language selectors should reuse those constants instead of hard-coding
`en`/`es` in multiple places.
Localized formatting should also use `AppLanguages.CultureFor`, so formatted
status text follows the selected app language instead of the machine's current
OS culture.

Settings normalization goes through `UserSettingsPolicy`: theme fallback,
language fallback, minimum window size, and incomplete window-position cleanup
belong there instead of in storage or WinUI code.
WinUI `ElementTheme` projection from `AppThemePreference` goes through
`ThemePreferenceMapper`; keep UI enum mapping out of the main view model.
Settings preference projection goes through `SettingsPreferenceDraft`, and
load/save orchestration goes through `SettingsPreferenceStore`; writes still use
`UserSettings.WithPreferences` so theme, language, and last selected Preset move
together while window placement is preserved.
Settings save command input should go through `SettingsSaveRequest`, so selected
theme/language/Preset-to-draft projection can move with a future
`SettingsViewModel`. Persisting that request should go through
`SettingsPreferenceStore.SaveRequestAsync`; command handlers should not reach
into request internals before saving.
`SettingsSaveRequest` should own applying its draft to loaded Settings and
exposing its last-selected-Preset id; external callers should not persist raw
Settings drafts directly.
Settings save should return `SettingsPreferenceSaveResult` so UI state updates
consume explicit saved visual-memory and write-failure output instead of
re-reading draft fields or mapping local-data exceptions after persistence.
Status text and saved visual-memory projection should stay on
`SettingsPreferenceSaveResult`, so `MainPageViewModel.SaveSettings` only
constructs the request, invokes the store, and applies the result.
Preset visual-memory writes go through `UserSettings.WithLastSelectedPreset` so
commands that only update dropdown memory cannot accidentally alter theme,
language, or window placement. App-side callers should use
`SettingsPreferenceStore` for silent best-effort visual-memory persistence and
recoverable local-data failure handling.

Window placement updates go through `UserSettings.WithWindowPlacement` so width,
height, x, and y are written as one complete settings update. MainWindow treats
placement restore/save as best-effort and uses `LocalDataErrorPolicy` to ignore
recoverable local settings failures during startup/shutdown.

WinUI commands that write app-managed local data should use
`LocalDataWriteGuard` for recoverable filesystem failures. The shared exception
policy starts in Core `LocalDataFileSystemErrors`; App `LocalDataErrorPolicy`
can add UI-specific cases such as window-placement argument cleanup while
keeping Preset, Settings, and window-placement filesystem semantics consistent.

## Error Strategy

Core should move toward structured errors:

```text
code
monitorKey
path
details
exception
```

UI should localize user-facing text.

Apply errors should cross Core/App boundaries as stable token-style
`ApplyErrorCodes`, not user-facing prose.
Technical exception messages may stay as diagnostic detail on low-level results,
but row status/localization must not parse those messages.
Known/unknown apply failure mapping goes through `ApplyErrorClassifier`; keep
fallback classification there instead of inside apply orchestration.
Apply progress counters, progress-event construction, and final
`ApplySessionResult` projection go through `ApplyRunTracker`; keep counter
mutation and result-shape duplication out of renderer/applier orchestration.
Single-monitor render/apply execution should stay in the focused
`WallpaperApplyService` step helper; the main apply loop should only select
monitors, coordinate cancellation/progress, and commit step results.
Target inclusion and target count should come from `ApplyTargetPlan`, not from
fresh lambdas in the loop.
The internal monitor-step result should remain separate from the public Apply
DTOs because it is an implementation detail of the service loop.
Apply preflight for ready/missing sources goes through `ApplyPreflight`; keep
missing-source detection and ready-target calculation out of WinUI command
handlers. Monitor-key sets should use `MonitorKeys.CreateSet` so
case-insensitive matching policy stays in one Core helper.
Cancellation is not an apply failure. `ApplyCanceledException` carries the
partial `ApplySessionResult`, so UI can keep already-applied monitor state
without showing false monitor errors.

Important errors:

- missing image source
- invalid color
- missing monitor
- render failure
- Windows interop failure
- app data write failure

## Extension Rules

When adding next features:

1. Add Core contract first.
2. Add fake/sample test path.
3. Add production Windows adapter.
4. Wire App after Core behavior is stable.
5. Run final build/tests once after the slice.
