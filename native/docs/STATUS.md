# Waller Native Status

Date: 2026-06-04

## Status Summary

The native project now has a real solution structure and enough code to start
moving in small slices.

This is not a finished MVP. It is now the base architecture plus real Windows
monitor/current-wallpaper detection, native image picking, PNG rendering,
placement modes, real Apply command wiring, and a detailed manual smoke
checklist for behavior that still depends on a real Windows session.

Latest verification:

- 2026-06-09: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit` passed after
  splitting Manage Presets modal commands into
  `MainPageViewModel.PresetManagement.cs`. Covered lints, package asset/script
  guards, solution build, packaged debug build, and 160 tests.
- 2026-06-09: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit` passed after
  splitting the localization catalog into `LocalizedText.Catalog.cs`. Covered
  lints, package asset/script guards, solution build, packaged debug build, and
  160 tests.
- 2026-06-09: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit` passed after
  extracting Settings command/load/options methods into a `MainPageViewModel`
  partial file. Covered lints, package asset/script guards, solution build,
  packaged debug build, and 160 tests.
- 2026-06-08: `scripts\Verify.ps1 -DisableNuGetAudit` passed lints, solution
  build, and 160 tests after monitor-row notification prefactor, then blocked
  at packaged launch smoke because `winapp` hit package registration conflict
  `0x80073D19`. Read-only follow-up
  `scripts\TestDevPackageRegistration.ps1 -AllUsers` found current user clean
  but could not inspect all-user registrations without elevation
  (`Access is denied.`).
- 2026-06-08: `scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit` passed after
  adding full-text tooltips for truncated dynamic UI text. Covered lints,
  package asset/script guards, solution build, packaged debug build, and 160
  tests.
- 2026-06-08: latest package gate is
  `scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit` after the local
  write recovery code guard. Output package:
  `artifacts\packages\Waller-dev-x64.msix`, size `78,698,590` bytes.
- 2026-06-08: latest full smoke still comes from
  `scripts\Verify.ps1 -DisableNuGetAudit` after adding `MainPageLocalState`.
  It launched AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`,
  process `Waller.Native.App`, window title `Waller`, and `Responding=True`.

Recent prefactor:

- 2026-06-09: Manage Presets modal commands now live in
  `MainPageViewModel.PresetManagement.cs`. `MainPageViewModel.Presets.cs` keeps
  save/save-as/load/selection flow, making the next Presets surface extraction
  narrower.

- 2026-06-09: English/Spanish UI copy now lives in
  `LocalizedText.Catalog.cs`, while `LocalizedText.cs` keeps formatting and
  domain/result text projection. This makes future `.resw` migration or extra
  languages less tangled with view-model status logic.

- 2026-06-09: source-generated property-change hooks now live in
  `MainPageViewModel.Changes.cs`, and `ResetPositionCommand` moved with the
  editor flow. The main view-model file is now a compact state/derived-property
  definition instead of a mixed state + command + change-handler file.

- 2026-06-09: shell initialization, current-session refresh, row/session
  refresh, modal close dispatch, and notification helpers now live in
  `MainPageViewModel.Shell.cs`. `MainPageViewModel.cs` now mostly holds state,
  derived properties, and source-generated property-change hooks.

- 2026-06-09: editor, source-selection, placement, and disconnected-monitor
  commands/helpers now live in `MainPageViewModel.Editor.cs`. The main
  view-model file now focuses on shared shell state and property notifications,
  while editor orchestration has a clear path toward a focused editor surface.

- 2026-06-09: Preset save/load/manage commands and helper flow now live in
  `MainPageViewModel.Presets.cs`. The main view-model file is reduced to the
  shared shell/editor surface while Preset orchestration has a clear extraction
  point for a future focused Presets surface.

- 2026-06-09: Apply commands and Apply run UI projection now live in
  `MainPageViewModel.Apply.cs`. `MainPageViewModel.cs` keeps less command
  orchestration, while Apply start/cancel/progress/result flow remains grouped
  for a future focused Apply surface.

- 2026-06-09: Settings command/load/options methods now live in
  `MainPageViewModel.Settings.cs`. `MainPageViewModel.cs` is smaller and the
  Settings surface has a clearer extraction point for a future focused
  `SettingsViewModel`.

- 2026-06-08: current and disconnected monitor row refreshes now share
  `MonitorRowNotificationGroups`. Row text/session refresh property lists live
  in one helper, so future preview/status fields are less likely to update on
  one row type and stay stale on the other.

- 2026-06-08: disconnected-monitor rows now reuse the same source-preview
  projection as current monitor rows. Missing Preset assignments show a compact
  image/color/empty/missing preview next to their saved monitor metadata,
  keeping future row visual changes in shared preview helpers instead of
  disconnected-row-only XAML.

- 2026-06-08: truncated dynamic text now exposes the full value in tooltips for
  the header summary, monitor rows, missing-monitor rows, selected-monitor
  editor title, and Apply progress.

- 2026-06-08: the header session summary now has a stable max width and
  ellipsis trimming, keeping the Preset dropdown and command buttons from being
  pushed by long monitor/preset state text.

- 2026-06-08: monitor rows, missing-monitor rows, selected-monitor editor
  title, and Apply progress now trim long dynamic text with ellipsis. This
  protects row action buttons, the right edit panel, and footer progress
  controls from long monitor names, paths, placement summaries, or status
  strings.

- 2026-06-08: `TestWinUICodeGuards.ps1` now rejects direct
  `LocalDataWriteGuard.IsRecoverable(...)` call sites outside the approved
  local-state helpers. New local write recovery paths should use
  `LocalDataWriteGuard.TryAsync` or the managed-preset mutation helper. The
  guard now uses a local relative-path helper instead of `Path.GetRelativePath`
  so violation reporting works on the current PowerShell runtime.

- 2026-06-08: `MainPageViewModel` now routes managed preset missing/write
  failure presentation through one helper. Rename, duplicate, and delete keep
  the same behavior, but future managed-preset actions no longer need to repeat
  the missing-preset refresh and local-write-failure status branches.

- 2026-06-08: `LocalDataWriteGuard.TryAsync` now returns typed fallbacks for
  recoverable local write failures. Preset save and settings save paths use it,
  and managed preset mutations share one local helper that preserves
  missing-preset semantics before recoverable filesystem fallback.

- 2026-06-08: `MainPageLocalState` now owns page-level local persistence
  operations for settings, preset save/load/manage, and rendered-cache cleanup.
  `MainPageViewModel` calls that service instead of passing stores into many
  helper functions, leaving future local-state behavior easier to add in one
  place.

- 2026-06-08: `MainPageViewModel` now receives `WallerLocalDataStores` as one
  dependency instead of separate Preset, Settings, and RenderedWallpaper stores.
  This keeps page-level state wiring aligned with the app-data facade and makes
  the next local-state additions cheaper.

- 2026-06-08: `WallerLocalDataStores` now creates Presets, Settings, and
  RenderedWallpapers stores from one app-data root. `WallerAppServices` and
  `MainWindow` use that facade, so window placement and page state cannot drift
  onto different local data paths as more state files are added.

- 2026-06-08: `LocalJsonFile` now owns recoverable JSON read fallback.
  `PresetStore` and `UserSettingsStore` use the shared helper, keeping corrupt,
  unsupported, locked, or unreadable local data recovery consistent before more
  local app state is added.

- 2026-06-08: preset menu refresh now reports when a requested persisted preset
  id is missing. `MainPageViewModel` uses that signal to clear
  `LastSelectedPresetId`, so startup does not repeatedly chase a deleted,
  corrupt, or unsupported preset file.

## Created Projects

```text
Waller.Native.slnx
  Waller.Native.App
  Waller.Native.Core
  Waller.Native.Tests
```

The solution uses `.slnx` because current .NET tooling generated that format.
Use `dotnet sln .\Waller.Native.slnx ...` for future project operations.

## Current Implementation

### App

Implemented:

- `App.xaml`
- `MainWindow.xaml`
- `MainPage.xaml`
- `MainPageViewModel`
- `MonitorRowViewModel`

The page currently shows:

- `Waller` header
- Preset dropdown
- Save / Save as / Manage / Settings / Apply all commands
- Save as modal
- Manage Presets modal
- monitor topology strip
- monitor rows
- source/placement/status summaries
- right edit panel
- inline status through `InfoBar`

Packaging identity now exposes `Waller` as the user-facing app name and
description, instead of template `Waller.Native.App` text. The package
publisher is `CN=Waller` instead of the template `CN=AppPublisher`. The
manifest keeps only the required full-trust capability for the desktop app.

Commands are wired:

- `ApplyAllCommand` renders and applies supported sources sequentially.
- `ApplyMonitorCommand` renders and applies one supported monitor source.
- Apply commands report progress while work runs.
- Apply progress exposes a Cancel command that requests cancellation and shows
  localized cancelled status.
- Footer status and Apply progress/cancel controls now live in separate footer
  columns, avoiding progress text/buttons overlapping status copy.
- Apply with no matching/current targets reports a friendly "nothing to apply"
  status instead of a 0-success/0-failure summary.
- Apply commands are disabled while an Apply operation is already running, and
  command handlers ignore concurrent Apply attempts before any preflight state
  changes.
- Core Apply monitor behavior now has a regression test for unknown monitor keys:
  no render output is created, Windows apply is not called, and the current
  session remains clean with a "nothing to apply" result.
- Apply and Refresh are disabled while any modal is open, so keyboard
  accelerators cannot run background session-changing actions behind a modal.
- Header shell commands and the image picker use `CanUseShellCommands`, so
  Save, Save as, Manage, Settings, Preset dropdown, and Choose image do not run
  behind an open modal while modal-local actions remain available.
- Modal scrims now use shared `WallerModalOverlayBrush` instead of repeated
  hard-coded overlay colors in page XAML.
- Main page surface corner radii now use WinUI `OverlayCornerRadius` and
  `ControlCornerRadius` theme resources instead of numeric XAML literals.
- App command-state notifications now flow through a shared view-model helper,
  keeping Apply, edit, shell, and Manage Presets enabled states in sync when
  Apply/modal state changes.
- Apply run setup/teardown now flows through small helpers that return an
  explicit `CancellationToken`, removing nullable cancellation-field access from
  apply service lambdas.
- Apply all/one-monitor ready-source request construction now goes through
  `ApplyRunRequest`, keeping service-target lambdas out of command handlers.
- Apply cancellation token ownership now lives in `ApplyRunState`, keeping CTS
  lifetime/cancel/dispose details out of `MainPageViewModel`.
- Apply exception-to-UI-state mapping now lives in `ApplyRunUiState`, keeping
  cancelled/operation-cancelled/unexpected-failure projection out of
  `MainPageViewModel.RunApplyAsync`.
- Modal-local actions now use `CanUseModalActions`, separating "modal action can
  run while a modal is open" from shell command and editor permissions.
- Main-surface monitor assignment edits now use `CanEditMonitorAssignment`, so
  source, image path, color, swatches, placement, and disconnected-monitor
  reassign/forget actions are disabled and ignored while Apply runs or any modal
  is open. Save as modal fields/actions use `CanUseModalActions`, keeping modal
  editing available without reopening main-surface assignment mutation.
- Repeated dependent-property notification groups now go through shared
  notification helpers and `ViewModelNotificationGroups`, reducing manual
  `OnPropertyChanged` clusters in the main view model.
- Delete confirmation target/message refresh now uses the same grouped
  notification helper as the modal visibility surface.
- Language refresh, modal visibility/open-state, and apply-progress visibility
  now also flow through `ViewModelNotificationGroups`, reducing remaining
  inline notification lists before future Settings/modal view-model splits.
- Escape/top-modal close dispatch now goes through `ShellModalClose`, keeping
  modal priority handling out of `MainPageViewModel.CloseTopModal`.
- Session summary refresh now uses a semantic grouped-notification helper
  instead of direct `OnPropertyChanged` calls.
- Monitor row and disconnected-monitor row refresh notifications now also use
  small multi-property helpers, keeping placement/source/status dependency
  refreshes in one place.
- WinUI `Visibility` projection now goes through a tiny `VisibilityStates`
  helper, keeping repeated Visible/Collapsed policy out of view models.
- Unexpected Apply pipeline failures now clear the progress state and show a
  friendly localized status instead of leaving the shell in applying state or
  exposing raw exception text.
- `ChooseImageCommand` opens a native image file picker.
- `SaveCommand` updates the selected local Preset or falls through to Save as.
- `SaveAsCommand` opens a small modal for naming a new local Preset before
  writing JSON.
- Save and Save as now share the same post-save session/menu/visual-memory
  refresh helper, so saved-session state cannot drift between both commands.
- Save and Save as selected-record/name-draft projection now goes through
  `PresetSaveCompletion`, keeping save-mode UI state decisions out of command
  handlers.
- Save and Save as preset construction/writes now go through
  `PresetSessionSave`, keeping `PresetFactory`, direct store writes, and local
  write-failure mapping out of command handlers.
- `ManagePresetsCommand` opens a modal for rename, duplicate, and confirmed
  delete.
- `RefreshCommand` is exposed in the header and reloads current Windows
  monitor/wallpaper state without restarting the app.
- Refresh is disabled and ignored while Apply is running so monitor/session
  state is not replaced mid-apply.
- Session-editing controls, Preset mutation controls, disconnected-monitor
  actions, and rendered-cache clear are disabled/ignored while Apply runs.
- Manage Presets modal mutation controls are also disabled while Apply runs, so
  an already-open modal cannot rename, duplicate, or delete Presets mid-apply.
- Manage Presets selection and required-name checks now go through shared view
  model helpers, keeping rename/duplicate/delete command guards consistent.
- Manage Presets rename, duplicate, and delete now go through
  `ManagedPresetMutation`, returning explicit missing/local-write-failed
  outcomes instead of mapping exceptions and store naming/load/delete details in
  the command handler.
- Manage Presets confirmed delete now goes through `ManagedPresetDelete`, so
  the command handler does not recompute whether the deleted Preset was the
  active session base or create the replacement selected-session result before
  refreshing Preset surfaces.
- Settings save is disabled/ignored while Apply runs, matching cache clear and
  other app-data mutation guards.
- `OpenSettingsCommand` opens a Settings modal for theme, language, and rendered
  cache cleanup.
- Settings rendered-cache cleanup now goes through `RenderedCacheCleanup`,
  keeping direct rendered-store calls out of the Settings command handler.
- Rendered cache cleanup now returns deleted/skipped counts, removes final PNG
  plus internal temp files, and tolerates locked or inaccessible cache files
  instead of crashing the Settings action.
- Rendered cache cleanup now recognizes only app-internal render temp files, so
  unrelated `.tmp` files under the rendered directory are preserved.
- Rendered cache cleanup now reports a failure when the rendered cache path is
  blocked by a file, instead of presenting a false successful 0-deleted clear.
- Rendered cache cleanup now also reports recoverable enumeration failures, so
  Settings cache clear does not crash when Windows blocks listing the folder.
- Rendered cache clear summaries now use `RenderedCacheClearResult.HasFailures`,
  keeping partial-clear status logic with the result DTO.
- App-side default services now use `WallerAppDataPaths` as the single source
  for the `%LOCALAPPDATA%\Waller` root.
- App-side default service wiring now lives in `WallerAppServices` instead of
  static factory methods inside `MainPageViewModel`.
- Last selected Preset is persisted when the user selects/saves/deletes Presets,
  then restored as dropdown visual memory on startup without auto-loading it
  over the current Windows state.
- Current Windows setup detection now also reads global `IDesktopWallpaper`
  position and maps it to initial placement (`Fill` -> Cover, `Fit` -> Contain,
  `Stretch` -> Stretch, `Center` -> Center, `Tile` -> Tile).
- Windows monitor display names now include display index plus shortened device
  id (for example `Monitor 1 - ABC`) so real monitor smoke notes can be matched
  back to Windows device paths.
- `DesktopWallpaperApplier` now asks the writer to set Windows wallpaper
  position to `Fill` when applying Waller-rendered PNGs, so the prerendered
  per-monitor fit/anchor/offset output is not re-fit by an older global Windows
  setting.
- When Windows reports no per-monitor wallpaper path, detection now maps
  `IDesktopWallpaper.GetBackgroundColor` to a solid-color source; if that read
  fails, the monitor stays empty/black.
- Invalid or relative wallpaper paths from Windows now map to empty source
  instead of failing startup detection.
- If `GetPosition` fails, detection keeps the monitor/source list and falls
  back to default placement instead of showing an empty app state.
- Settings load before startup status text is generated, so the first visible
  session/detection message uses the saved language.
- Page startup initialization is now caught by `MainPage.OnLoaded`; unexpected
  initialization failures show localized status text instead of escaping the
  async Loaded handler.
- Session summary marks restored Preset dropdown selection as visual-only when
  the active session is still current Windows state.
- Preset dropdown selection now flows through `SelectedPresetSessionLoader` and
  `SelectedPresetSessionFactory`, keeping current setup, loaded Preset, missing
  Preset, and deleted-active-Preset session/draft/memory projection in one
  place.
- Preset dropdown loads now use a view-model version guard, so a slower stale
  async load cannot overwrite a newer user selection or refreshed menu state.
- Preset dropdown load failures are now caught inside the fire-and-forget task
  and surfaced with localized status text instead of becoming unobserved task
  exceptions.
- Renaming the active Preset now flows through `ActivePresetSession.RenameActive`,
  keeping selected record, session identity, and name draft projection together.
- English/Spanish UI labels are exposed through a lightweight app localizer and
  switched from the Settings language selector.
- Localized format strings now use the selected app language culture through
  Core `AppLanguages.CultureFor`, instead of inheriting the OS current culture.
- Apply result summary text, including skipped missing-source count, is formatted
  by `LocalizedText` instead of being assembled in the main view model.
- `ApplySessionResult.HasAppliedOutcome` now separates monitors that were
  applied/failed from monitors that were only skipped, so all-missing-source
  apply attempts report "nothing applied" instead of a 0-success/0-failure
  finished summary. `HasAnyOutcome` still covers skipped-only results.
- Apply progress text is also formatted by `LocalizedText`, so progress/status
  copy stays with localization instead of apply orchestration.
- Apply preparing/progress/result/cancel/failure text now flows through
  `ApplyTextPresenter`, keeping apply-specific text projection out of
  `MainPageViewModel`.
- Preset save/load/manage status text now flows through `PresetTextPresenter`,
  keeping preset-specific prompt/result copy out of `MainPageViewModel`.
- Selected-source warning and rendered-cache clear summaries now go through
  `LocalizedText`, keeping remaining warning/status copy out of the main view
  model.
- Edit validation exception messages now go through `LocalizedText`, keeping
  user-facing validation copy out of assignment editing.
- Editor and disconnected-monitor status text now flows through
  `MonitorEditTextPresenter`, preparing the current inline editor commands for
  a future focused edit view model.
- Current-session, Settings, local-write, and rendered-cache status text now
  flows through `ShellStatusTextPresenter`, keeping broad shell copy out of
  command handlers.
- Monitor row formatting for resolution, bounds, placement, and status now goes
  through `LocalizedText`; current and disconnected row source summaries now go
  through `MonitorSourceText`, keeping row view models focused on projection
  rather than copy/string assembly.
- Runtime language refresh for row text and the Current setup Preset label now
  goes through `LocalizedSurfaceRefresh`, keeping cached localized-surface
  updates out of the main language-change handler.
- Active Session header summary formatting now goes through `LocalizedText`,
  keeping Preset name, modified/disconnected/visual-only suffix assembly out of
  the main view model.
- Row source/status labels show localized saved/unsaved and missing-source
  state.
- Main status/progress messages for Preset, Settings, and Apply flows use the
  same lightweight localizer.
- The monitor list has a localized empty state and the icon-only Settings
  button has tooltip/accessibility metadata.
- Presets with assignments for disconnected monitors now show a localized
  disconnected monitors section instead of only a header count.
- Disconnected monitor image assignments now show localized missing-source text
  when the original file no longer exists.
- Disconnected assignments can be forgotten from the active session and then
  persisted by saving the Preset.
- Disconnected assignments can be reassigned to the selected current monitor
  before saving.
- Disconnected reassignment now normalizes the copied placement and uses the
  same case-insensitive monitor-key rules for source and target lookup.
- Disconnected monitor forget/reassign commands now go through
  `DisconnectedMonitorEdit`, keeping row-to-session-key mapping out of the main
  view model before the editor split.
- Disconnected monitor edits now return `DisconnectedMonitorEditResult`,
  carrying optional replacement session plus localized status text for
  forget/reassign/missing-target outcomes.
- Main monitor/work columns are scrollable so disconnected sections and edit
  controls stay reachable in smaller windows.
- Primary shell commands now expose tooltip/accessibility metadata beyond the
  icon-only Settings button.
- Main shell, editor, Preset, and Settings interactive controls now expose
  stable `AutomationProperties.AutomationId` values for future WinUI UI
  automation.
- `scripts\TestXamlAccessibility.ps1` now lints `MainPage.xaml` for missing
  AutomationIds on supported interactive controls, invalid AutomationId token
  formats, duplicate AutomationIds within the same XAML scope, and missing
  `UpdateSourceTrigger=PropertyChanged` on TwoWay TextBox `x:Bind`. It also
  blocks hard-coded `CornerRadius` and `Background` color literals in
  `MainPage.xaml`; `Verify.ps1` runs it before build/test/smoke work.
- The XAML accessibility lint also requires monitor/disconnected-monitor row
  DataTemplate roots with row action buttons to expose `AutomationProperties.Name`.
- `scripts\TestXamlLocalization.ps1` blocks hard-coded user-visible `Text`,
  `Content`, `Header`, `Message`, tooltip, and automation-name strings in
  `MainPage.xaml` so new visible copy must flow through `LocalizedText`. The
  app title `Waller` is the only allowed literal brand label, and `Verify.ps1`
  runs this localization lint as its own step.
- `scripts\TestWinUICodeGuards.ps1` blocks inline async `Loaded` handlers in
  app C# so startup work stays behind named handlers with localized failure
  status instead of unobserved async lambdas. It also blocks hard-coded
  `StatusText`/`ApplyProgressText` string literals so visible status copy keeps
  flowing through localized presenters.
- `scripts\TestJsonCodeGuards.ps1` blocks direct `JsonSerializer` persistence
  calls outside `LocalJsonFile`, so Preset/Settings local data stays on
  `WallerJsonContext` source-generated metadata and avoids Release trim drift.
- Edit panel and Preset/Settings modals have initial tab ordering and named
  modal action buttons.
- Monitor and disconnected-monitor list item roots expose accessible display
  names, improving screen-reader context before entering row action buttons.
- Manage Presets and Settings move keyboard focus into their first useful
  controls when opened.
- Delete confirmation moves keyboard focus to Confirm delete when opened.
- Manage Presets list/name/rename/duplicate/delete controls are disabled while
  delete confirmation is open, so the confirmation target cannot change behind
  the confirm action.
- Delete confirmation captures the target Preset id/name and shows the Preset
  name in the confirmation text before deletion.
- Delete confirmation target state now lives in `PresetDeleteConfirmation`, so
  the captured id/name and localized confirmation message stay together while
  the main view model moves toward a future `ManagePresetsViewModel`.
- Escape closes the top modal layer: delete confirmation first, then Manage,
  Save as, or Settings.
- Escape is only handled while a modal is open, so normal focused controls keep
  their default Escape behavior outside modal flows.
- Primary shell commands expose keyboard accelerators: Ctrl+S Save,
  Ctrl+Shift+S Save as, Ctrl+M Manage Presets, Ctrl+R Refresh, Ctrl+I Settings,
  and Ctrl+Enter Apply all.
- The topology strip now scales monitor tiles from real monitor bounds,
  including negative coordinates, instead of using equal placeholder cards.
- Topology-strip coordinate normalization now lives in Core
  `MonitorTopologyLayout`, keeping the WinUI view model out of monitor geometry
  math.
- Monitor rows now use lightweight visual source previews: image thumbnails
  when the file exists, solid-color fill, black empty state, and text fallback
  for missing images.
- Monitor row Edit buttons now explicitly select the row and focus the edit
  panel workflow instead of being visual-only.
- The topology strip now dims unselected monitor tiles and thickens the selected
  monitor tile border, so topology selection matches the selected monitor row.
- Selected monitor row state now flows through `MonitorRowSelection`, keeping
  row/tile selection flag updates out of `MainPageViewModel`.
- The topology strip is hidden when there are no detected monitors, leaving the
  localized no-monitors empty state as the only primary content.
- Common validation and Windows detection fallback messages now use friendly
  localized copy instead of raw exception text.
- Monitor row Apply errors map known technical failures to friendly localized
  summaries instead of exposing raw paths/exceptions.
- Apply pipeline stores stable friendly error categories on monitor rows, so
  renderer/Windows exception details do not leak into session state.
- Renderer and Windows applier failures now flow through stable Apply error
  codes before localization; monitor-row copy no longer depends on parsing
  technical error text.
- Apply error category tokens are centralized in `ApplyErrorCodes`, so Core,
  App localization, and tests do not drift on prose-like magic strings.
- Apply missing-source preflight now lives in Core
  `WallpaperApplyService.ApplyMonitorReadySourceAsync`,
  `WallpaperApplyService.ApplyAllReadySourcesAsync`, and `ApplyPreflight`, so
  the UI no longer builds its own skip predicate before applying ready
  monitors.
- Apply-all with only missing image sources now reports all targets as skipped,
  marks each monitor with `MissingImageSource`, and does not create render
  output or call Windows apply. The user-facing summary says nothing was applied
  plus the skipped count.
- `ApplyPreflightResult` now carries both ready monitor keys and skipped
  monitor keys, so `WallpaperApplyService` consumes one Core apply plan instead
  of recreating missing-source predicates around preflight output.
- `ApplyPreflightResult` also exposes ready/skipped boolean helpers, keeping
  call-site flow checks out of raw set counts.
- `ApplyPreflightResult.SkippedCount` now feeds apply summaries, keeping
  skipped-count reporting with preflight output.
- Apply target selection now flows through internal `ApplyTargetPlan`, covering
  all monitors, one monitor, preflight ready keys, and filtered apply with the
  same monitor-key comparison rules.
- Apply known/unknown failure mapping now goes through `ApplyErrorClassifier`,
  keeping fallback error classification out of the apply orchestration loop.
- Apply progress counters and progress-event construction now go through
  `ApplyRunTracker`, keeping `WallpaperApplyService` focused on render/apply
  sequencing.
- Single-monitor render/apply execution inside `WallpaperApplyService` now goes
  through a focused step result, keeping renderer/applier failure mapping out of
  the main apply loop.
- Public Apply DTOs (`ApplySessionResult`, `ApplyProgress`,
  `ApplyPreflightResult`) and the internal monitor step result now live in
  focused files instead of inside service/preflight implementations.
- Apply cancellation now propagates instead of being converted into a monitor
  apply-error state. It carries partial apply results, so already-applied
  monitors stay visible after cancel without false failures.
- Monitor row localization maps those stable Apply error categories directly,
  without parsing raw technical exception text.
- `MonitorApplyStatus` no longer has a separate unused `Missing` state;
  missing image sources use `Error` plus `ApplyErrorCodes.MissingImageSource`.
- Core monitor-session state transitions now go through `MonitorSession` helper
  methods for pending assignment, applying, applied, and apply-error states.
- Saving an active session to a Preset now clears dirty state through
  `ActiveSession.WithSavedPreset`, instead of duplicating monitor/session dirty
  reset logic in the WinUI view model.
- Settings theme/language dropdowns show localized display labels rather than
  raw enum or language-code values.
- Theme preference to WinUI `ElementTheme` mapping now goes through
  `ThemePreferenceMapper`, keeping XAML theme projection out of the main view
  model.
- Settings and editor option lists now go through `LocalizedOptionCatalog`, so
  enum/language option projection stays out of the main view model.
- Full option-list refreshes now use `OptionItems.ReplaceAndSelect`, keeping
  replacement and selected-value restoration in one helper.
- Supported language-code normalization now goes through Core `AppLanguages`,
  shared by Settings persistence and the WinUI language selector.
- Supported language culture lookup also goes through `AppLanguages`, keeping
  formatted UI text aligned with the selected app language.
- Settings normalization now goes through Core `UserSettingsPolicy`, centralizing
  theme fallback, language fallback, minimum window size, and incomplete window
  position cleanup.
- Settings preference writes now go through `UserSettings.WithPreferences`,
  preserving window placement while updating theme, language, and last selected
  Preset together.
- Last selected Preset visual-memory writes now go through
  `UserSettings.WithLastSelectedPreset`, preserving Settings preferences and
  window placement.
- App-side Settings preference load/save and last-selected Preset memory now
  flow through `SettingsPreferenceStore`, keeping settings-store orchestration
  out of `MainPageViewModel`.
- Settings save command input now goes through `SettingsSaveRequest`, keeping
  selected theme/language/Preset-to-draft projection out of the command handler.
- Settings save now returns `SettingsPreferenceSaveResult`, so the view model
  updates visual Preset memory from saved output and consumes explicit
  write-failure state instead of catching local-data exceptions inline.
- Edit panel source, fit, and anchor dropdowns show localized display labels
  instead of raw enum values.
- Unknown status/source/fit/anchor fallbacks now show friendly localized
  fallback copy instead of raw enum names, and the WinUI code guard blocks new
  `_ => value.ToString()` UI fallbacks.
- Fit, anchor, X/Y position, and Reset position now use a dedicated
  `CanEditPlacement` rule, so image placement editing is available only when
  the selected source is Image. Empty and SolidColor sources remain valid while
  irrelevant placement controls stay disabled.
- Preset dropdown collection replacement now goes through `PresetMenuLists`;
  main Preset dropdown refresh goes through `PresetMenuRefresh`, and Manage
  Presets modal refresh goes through `ManagedPresetList`, keeping store
  listing/projection/selection fallback rules out of command handlers.
- Preset listing now uses stable ordinal case-insensitive name ordering with id
  tie-breaks, so dropdown order does not drift with current UI culture.
- Preset default-name formatting, trimming/validation, and duplicate-name
  derivation now go through Core `PresetNames`, keeping name policy out of the
  WinUI view model.
- Dropdown option refresh and selected-option lookup now use shared `OptionItems`
  helpers, reducing repeated clear/add and equality lookup logic in
  `MainPageViewModel`.
- Monitor row and disconnected-monitor placement summaries use localized fit
  and anchor labels instead of raw enum values.
- Edit panel shows Image path/Choose image only for Image source, and Color
  only for SolidColor source.
- Edit panel field projection and source/placement reconstruction now go through
  `MonitorEditDraft`, keeping assignment-edit conversion out of the main view
  model.
- Image picker and color swatch source changes now go through
  `MonitorSourceSelectionFactory`, keeping source-kind/image/color field
  projection out of individual command handlers.
- Editor field application to `ActiveSessionEditor` now flows through
  `MonitorAssignmentUpdate`, keeping source/placement conversion plus missing
  image and invalid-value outcomes out of the main view model before future
  editor extraction.
- Image source paths are normalized and must be full local paths before they
  enter a Preset/session assignment.
- Image picker extensions now come from Core `WallpaperImageFileTypes`, covering
  common wallpaper formats (`jpg`, `jpeg`, `png`, `bmp`, `webp`, `gif`, `tif`,
  `tiff`, `heic`, `heif`) without hard-coded picker-only lists.
- `WallpaperPlacement` now carries optional X/Y percent offsets with default
  `0,0`, preparing the requested image-position workflow without breaking older
  Preset JSON.
- Image rendering applies placement offsets within safe bounds, so Cover crops
  can shift position without revealing black bands.
- The edit panel now exposes compact X/Y position `NumberBox` controls
  (-100..100) for per-monitor wallpaper offsets.
- The edit panel now includes a compact Reset position command that returns X/Y
  offsets to `0,0` through one guarded view-model update, avoiding a transient
  half-reset assignment.
- Placement summaries show non-zero offsets for monitor rows and disconnected
  assignments.
- Monitor row image thumbnails now use a placement-aware `ImageBrush`, so
  preview fit and anchor follow the selected wallpaper placement instead of
  always using a fixed cover crop.
- Thumbnail placement mapping now lives in `PlacementPreview`; large X/Y
  offsets also nudge thumbnail alignment toward the crop side shown by final
  rendering.
- Placement offset clamping now lives in Core `WallpaperPlacement` helpers and
  is reused by app drafts, active-session creation/editing, rendering, Preset
  creation, and Preset saving.
- Preset JSON save/load tests now cover offset roundtrip and out-of-range
  offset normalization.
- Active Session creation and assignment editing now normalize placement
  offsets before storing desired assignments, so out-of-range values cannot sit
  in editable session state waiting for later save/render cleanup.
- SolidColor editing includes a native ColorPicker, validated hex input, and
  quick swatches for common wallpaper colors.
- SolidColor quick swatches now come from `ColorSwatchCatalog`, keeping palette
  choices out of `MainPageViewModel`.
- SolidColor hex normalization and RGB parsing now go through Core
  `ColorHexValue`, so App previews and Core rendering share one color policy.
- Selecting Image without a path keeps the editor on Image without mutating the
  saved monitor assignment until a path is chosen.
- Native image picker results now project through `ImageSelectionDraft`, keeping
  cancel handling and selected-file display names out of `MainPageViewModel`.
- Monitor selection refreshes editor fields without marking the selected
  monitor dirty.
- Re-selecting the same source/placement no longer marks the session dirty.
- `MainPageViewModel` now uses a small `RefreshSessionSurface` helper for
  changes that must refresh both monitor rows and the session summary.
- Apply result presentation now uses the same session-surface refresh helper, so
  applied/error row state and session-summary notifications stay grouped.
- Manage Presets shows a friendly empty state and handles blank or missing
  rename/duplicate targets without crashing. Missing managed Presets refresh
  both the modal list and main dropdown so stale entries disappear together.
- Local Preset and Settings writes now catch locked/inaccessible app-data
  failures and show friendly localized status instead of crashing the app.
- App-side local data recovery policy now goes through `LocalDataErrorPolicy`,
  and local data write handling goes through `LocalDataWriteGuard`, so Preset,
  Settings, and window-placement paths share recoverable failure semantics.
- Recoverable local filesystem errors now start from Core
  `LocalDataFileSystemErrors`, with App `LocalDataErrorPolicy` delegating to the
  shared policy before adding window-placement-specific cases.
- Local Preset and Settings JSON saves now use shared temp-file replacement, so
  a failed replace leaves the previous JSON readable.
- Manual verification checklist covers launch contract, startup detection,
  topology, source previews, edit panel, Presets, Settings, disconnected
  monitors, Apply, placement, and accessibility/keyboard behavior.
- Corrupt, parseable-invalid, unsupported-schema, locked, or inaccessible local
  Preset JSON files are skipped during list/load so one bad file does not block
  the app or add blank/stale menu entries. Invalid assignment lists are rejected
  before matching, instead of relying on null-reference failures.
- Loaded Preset assignment sources are normalized through `WallpaperSource`, so
  invalid image paths or color values are rejected before render/apply.
- Loaded Preset assignment monitor identities must have non-empty monitor keys
  and positive dimensions, keeping invalid fallback-match geometry out of the
  active session.
- Loaded Preset timestamps are normalized when older/corrupt JSON omits them or
  has `updatedAt` earlier than `createdAt`, keeping app-managed metadata sane.
- Presets with duplicate assignments for the same monitor key do not crash
  matching; the first assignment is used and duplicates are ignored.
- Duplicate Preset assignments are ignored with the same case-insensitive
  monitor-key policy used by editing, preflight, and Apply paths, so legacy JSON
  casing drift cannot create phantom disconnected assignments.
- Applying a Preset now runs assignments through the shared normalization policy
  before matching, so older JSON cannot inject out-of-range placement offsets
  into Active Session state.
- Saving a Preset normalizes duplicate assignments for the same monitor key, so
  app-managed JSON stays clean after load/save cycles.
- Preset creation from an active session now uses the same assignment
  normalization policy as Preset saving, so current monitors win over stale
  disconnected duplicates even when monitor-key casing drifts.
- Core monitor-key equality now goes through `MonitorKeys`, keeping Preset,
  editing, missing-source, and Apply paths on the same case-insensitive rules.
- Preset fallback matching now goes through `MonitorIdentityMatcher`, so
  monitor-key drift picks the closest same-resolution/near-position assignment
  deterministically instead of depending on JSON order.
- Rendered wallpaper cache file names now sanitize and cap monitor-key prefixes
  and include a short hash, preventing invalid file names and sanitized-name
  collisions from Windows monitor device paths.
- Rendered PNG writes now use the same shared temp-file replacement helper as
  local JSON saves, so cancellation or write failure does not leave partial final
  PNG output behind; Clear cache also removes stale temp render files.
- Image-file existence and file-name display now go through `WallpaperSourceFiles`,
  keeping Core preflight, selected-row warnings, current monitor rows, and
  disconnected monitor rows on the same missing-source rules.
- Monitor row image preview visibility now uses `WallpaperSourceFiles` directly,
  so visibility checks do not instantiate `BitmapImage` previews.
- Monitor row source preview brush creation now lives in `MonitorSourcePreview`,
  keeping image thumbnail construction out of `MonitorRowViewModel` getters.
- Rendering helpers are split out of `BasicPngWallpaperRenderer`: image
  placement, pixel buffers, RGB color conversion, image decoding, and PNG writing
  now live in focused internal files.
- Corrupt, locked, inaccessible, or unsupported Settings JSON falls back to safe
  defaults.
- Settings are normalized on save and load: language codes are canonicalized,
  minimum window size is enforced, and incomplete window positions are dropped.
- Window placement restore/save is tolerant of local settings failures, so a
  corrupt, locked, or inaccessible settings file should not crash startup or
  shutdown.

Startup behavior:

- app tries `WindowsMonitorDetector`
- app reads monitor IDs, bounds, and current wallpaper paths through
  `IDesktopWallpaper`
- app falls back to `EmptyMonitorDetector` if Windows detection fails, so
  production startup shows an empty/no-monitors state instead of fake displays
- primary detection and fallback loading now flow through `CurrentSessionLoader`,
  so release fallback policy can change without rewriting startup UI flow

Packaged launch smoke:

- 2026-06-07: `BuildAndRun.ps1 ... -Detach` launched the packaged app through
  `winapp`.
- Returned AUMID: `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_1z32rh13vfry6!App`.
- Process `Waller.Native.App` responded with main window title `Waller`.
- 2026-06-07: after startup fallback/settings-order changes,
  `BuildAndRun.ps1 ... -Detach` launched again. Process `48296` responded with
  main window title `Waller`, then was closed from the smoke script.
- `scripts\SmokeLaunch.ps1` now makes this check repeatable: build, detached
  launch, `winapp` JSON parse, process/title/responding verification, cleanup.
- `scripts\SmokeLaunch.ps1` now reports `winapp` JSON `Error` directly when
  launch/package registration fails, instead of masking it as a missing
  `ProcessId`.
- `scripts\TestDevPackageRegistration.ps1` provides a read-only package
  registration diagnostic for current-user and optional all-user checks before
  deciding whether explicit uninstall cleanup is appropriate.
- The package-registration diagnostic returns exit code `3` when current-user
  state is clean but all-user inspection is blocked by permissions, so smoke
  blockers are not mistaken for a clean result.
- The smoke script keeps launched process cleanup in `finally`, so failed
  launch assertions should not leave the app running.
- `scripts\Verify.ps1` now runs the standard local gate. Full mode runs XAML
  accessibility lint, solution build, tests, and packaged launch smoke;
  `-SkipSmoke` runs lint, solution build, packaged build without launch, and
  tests.
- `scripts\Verify.ps1` now treats non-zero native command exit codes as failed
  steps, so compiler/test errors cannot be followed by a misleading success
  banner.
- Local build scripts support opt-in `-DisableNuGetAudit` for restricted-network
  environments where NuGet vulnerability-audit data cannot be reached and
  creates `NU1900` noise.
- `Verify.ps1 -DisableNuGetAudit` now passes the same restore policy through
  packaged launch smoke, avoiding a nested smoke-build audit mismatch.
- `Verify.ps1 -Release` also runs `scripts\BuildRelease.ps1`; `-Platform`
  selects `x64`, `x86`, or `ARM64`.
- `Verify.ps1 -Package` also runs `scripts\PackageDevMsix.ps1`; if both
  `-Release` and `-Package` are provided, the package step owns the Release
  build to avoid duplicate Release compilation.
- Release/package helper scripts now check child process exit codes explicitly,
  so nested PowerShell/MSIX failures cannot be followed by a misleading package
  success banner.
- 2026-06-07: after changing package publisher to `CN=Waller`,
  `Verify.ps1` passed and `winapp` returned AUMID
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`.
- 2026-06-08: after modal/Apply guard updates, `scripts\SmokeLaunch.ps1
  -DisableNuGetAudit` passed. `winapp` returned the same AUMID and process
  `16820`; `Waller.Native.App` responded with main window title `Waller` before
  smoke cleanup.
- 2026-06-08: after view-model prefactors, `scripts\SmokeLaunch.ps1
  -DisableNuGetAudit` passed outside the restricted sandbox. `winapp` returned
  AUMID `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App` and process
  `38596`; `Waller.Native.App` responded with main window title `Waller`.
- 2026-06-08: after Core hardening, `scripts\SmokeLaunch.ps1
  -DisableNuGetAudit` passed outside the restricted sandbox. `winapp` returned
  the same AUMID and process `15728`; `Waller.Native.App` responded with main
  window title `Waller`.
- 2026-06-08: after App local-data policy prefactor and defensive Core/cache
  hardening, `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the
  restricted sandbox. `winapp` returned AUMID
  `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App` and process
  `15644`; `Waller.Native.App` responded with main window title `Waller`.
- 2026-06-08: after image picker file-type policy updates,
  `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the restricted
  sandbox. `winapp` returned the same AUMID and process `10304`;
  `Waller.Native.App` responded with main window title `Waller`.
- 2026-06-08: after current-session and Preset-selection prefactors,
  `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the restricted
  sandbox. `winapp` returned the same AUMID and process `59920`;
  `Waller.Native.App` responded with main window title `Waller` and
  `Responding=True`.
- 2026-06-08: after monitor source-selection prefactor,
  `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the restricted
  sandbox. `winapp` returned the same AUMID and process `22980`;
  `Waller.Native.App` responded with main window title `Waller` and
  `Responding=True`.
- 2026-06-08: after row-template accessibility lint hardening,
  `scripts\SmokeLaunch.ps1 -DisableNuGetAudit` passed outside the restricted
  sandbox. `winapp` returned the same AUMID and process `11628`;
  `Waller.Native.App` responded with main window title `Waller` and
  `Responding=True`.

### Core

Implemented model folders:

```text
Models/
Sessions/
Presets/
Rendering/
Settings/
Windows/
Contracts/
```

Implemented domain concepts:

- `MonitorBounds`
- `MonitorIdentity`
- `MonitorSnapshot`
- `WallpaperSource`
- `WallpaperPlacement`
- `MonitorSession`
- `ActiveSession`
- `Preset`
- `RenderedWallpaper`
- `ApplyResult`
- `UserSettings`

Implemented service concepts:

- `ActiveSessionFactory`
- `ActiveSessionEditor`
- `PresetMatcher`
- `PresetStore`
- `RenderedWallpaperStore`
- `UserSettingsStore`
- `IMonitorDetector`
- `IWallpaperApplier`
- `DesktopWallpaperInterop`
- `WindowsMonitorDetector`
- `DesktopWallpaperApplier`
- `BasicPngWallpaperRenderer`
- `WallpaperApplyService`
- `SampleMonitorDetector`
- `EmptyMonitorDetector`

### Tests

Implemented first Core tests:

- Active Session creation from sample monitor detector.
- Empty monitor fallback creates an empty Active Session.
- assignment edit returns new session.
- Preset exact match.
- missing Preset assignment preservation.
- Preset JSON roundtrip and failed-replace protection.
- corrupt/invalid/unsupported/locked Preset JSON skip/fallback.
- corrupt/locked/unsupported Settings JSON fallback and failed-replace
  protection.
- Empty wallpaper path mapping.
- Wallpaper path mapping to Image source.
- Windows monitor detection mapping is now tested through an internal reader
  adapter, including per-monitor wallpaper paths and background-color fallback.
- Apply failure before Windows interop when rendered file is missing.
- Desktop wallpaper apply success/failure mapping is now tested through an
  internal writer adapter, without calling the real COM API.
- rendered wallpaper file-name sanitization/collision guard.
- shared atomic file writer completion/failure behavior.
- rendered PNG atomic write/cancellation guard.
- rendered cache temp-file cleanup.
- monitor topology scaling for negative-coordinate layouts.
- SolidColor PNG render output dimensions.
- Pixel buffers now validate positive dimensions and exact RGB byte length
  before render output reaches PNG writing.
- Image placement math now lives in `ImagePlacementPlan`, with direct coverage
  for Cover offset crop, Contain bands, Stretch, Center, and Tile planning
  before pixel drawing.
- Apply service success path through renderer/applier.
- Apply monitor key matching is case-insensitive, matching Preset/Editor
  semantics.
- Image source failure before Windows apply.

## Current Fake/Placeholder Areas

These placeholders are intentional:

- `SampleMonitorDetector` returns deterministic monitor data.
- `SampleMonitorDetector` is development/test-only.

Do not mistake placeholders for design uncertainty. They are slice boundaries.

## Known Setup Limitation

`Microsoft.Windows.CsWin32` was not added yet because package installation was
blocked during the first setup pass. Current code uses focused manual
`IDesktopWallpaper` COM interop. The native interop contract file still exists:

```text
Waller.Native.Core\Contracts\NativeMethods.txt
```

When package access is available, we can either keep manual COM if stable or add
CsWin32 to Core and generate interop from that file.

## Current Risk Register

### Real Windows wallpaper APIs

Risk:

`IDesktopWallpaper` COM interop and packaged WinUI identity can fail in ways
that look like app bugs.

Mitigation:

Keep Windows interop behind `IMonitorDetector` and `IWallpaperApplier`.
Keep `WindowsMonitorDetector` raw COM reads behind its internal reader adapter
so monitor/source mapping can be tested without querying Windows.
Keep `DesktopWallpaperApplier` raw COM writes behind its internal writer
adapter so success/failure behavior can be tested without mutating Windows.
The writer contract includes the Windows position because Waller applies
already-rendered monitor-sized PNGs and should force `Fill` during apply.
Validate detector first without apply.

### Per-monitor placement

Risk:

Windows wallpaper position is effectively global, so using Windows placement
settings for per-monitor behavior will not satisfy product requirements.

Mitigation:

Prerender final per-monitor PNG files with placement already baked in.

### Missing source images

Risk:

Presets store original full image paths. Those files can disappear.

Mitigation:

Detect missing source before render/apply. Fail only affected monitor. Preserve
Preset data.

Implementation note:

Missing-image Apply preflight lives in Core `ApplyPreflight`, not in the UI view
model. Apply-monitor and Apply-all use Core ready-source paths so missing image
monitors are marked as skipped/error while ready monitors can still apply.

### Monitor identity drift

Risk:

Display IDs can change after unplug/replug, GPU updates, dock changes, or
Windows updates.

Mitigation:

Store stable key plus fallback metadata. Match exact key first, then closest
same-resolution/near-position fallback.

### Packaged launch confusion

Risk:

Double-clicking the raw exe can do nothing.

Mitigation:

Always run through `BuildAndRun.ps1`.

### Packaging readiness

Current strategy:

Use `docs\PACKAGING.md` as the operational source for Release, certificate,
MSIX, install, and uninstall workflow.

Start with a repeatable unsigned Release build before generating certificates or
installable MSIX artifacts. `scripts\BuildRelease.ps1` runs
`BuildAndRun.ps1 -SkipRun /p:Configuration=Release /p:Platform=x64` by
default. Signed MSIX work remains separate because certificate trust can require
administrator elevation.

`scripts\PrepareDevCertificate.ps1` can generate a local development PFX and
public CER under ignored `artifacts\signing\` without installing/trusting it.
Certificate trust remains an explicit elevated step.
2026-06-07 local run generated `CN=Waller` dev cert thumbprint
`3403C36B7E93446F269873593B379EC2419D5F17`.
`scripts\PackageDevMsix.ps1` builds Release, ensures the dev certificate exists,
and creates a signed development MSIX under ignored `artifacts\packages\`
without installing it. It runs `scripts\InspectDevMsix.ps1` to verify package
manifest identity and signing certificate subject after packaging.
2026-06-08 local package gate passed outside the restricted sandbox with
`Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit`: XAML accessibility lint,
XAML localization lint, WinUI code guards, solution build, packaged debug
build, 138 tests, Release build, signed dev MSIX creation, and MSIX inspection.
Output: `artifacts\packages\Waller-dev-x64.msix`, size `78,682,578` bytes.
Identity `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`, publisher `CN=Waller`,
version `1.0.0.0`, architecture `x64`. Signature remained
`UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17` because the dev cert
is generated but not trusted.
`scripts\TestDevCertificateTrust.ps1` checks CurrentUser and LocalMachine Root
stores for the dev certificate thumbprint without modifying cert stores.
`scripts\InstallDevMsix.ps1` performs inspect + trust preflight by default and
only calls `Add-AppxPackage` when `-Install` is explicitly passed.
`scripts\UninstallDevPackage.ps1` reports installed Waller development packages
by default and only calls `Remove-AppxPackage` when `-Uninstall` is explicitly
passed.
`scripts\SetPackageVersion.ps1` reads package identity/version by default and
updates `Package.appxmanifest` only when `-Version` is provided. It validates
the MSIX four-part numeric version format before writing.
2026-06-08 local package gate passed with
`Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit` outside the restricted
sandbox after sandbox restore hit `NU1301`. It produced
`artifacts\packages\Waller-dev-x64.msix` (`78,577,711` bytes).
`Get-AuthenticodeSignature` sees signer `CN=Waller` and thumbprint
`3403C36B7E93446F269873593B379EC2419D5F17`; status is untrusted until the dev
certificate is installed.
2026-06-08 local no-smoke verification passed after Manage Presets mutation
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after Preset save/settings save
prefactors with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, solution build, packaged build,
and 138 tests.
2026-06-08 local no-smoke verification passed after selected-Preset loader
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after monitor-assignment update
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after monitor-row selection
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after rendered-cache cleanup
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after main Preset menu refresh
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after apply session-surface
refresh prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, solution build, packaged build,
and 138 tests.
2026-06-08 local no-smoke verification passed after Preset dropdown stale-load
guard with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint,
XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after Preset dropdown load-failure
handling with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after monitor-row accessibility
name updates with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, solution build, packaged build,
and 138 tests.
2026-06-08 local no-smoke verification passed after row-template accessibility
lint hardening with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, solution build, packaged build,
and 138 tests.
2026-06-08 local no-smoke verification passed after startup initialization
failure handling with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, solution build, packaged build,
and 138 tests.
2026-06-08 local no-smoke verification passed after adding WinUI code guards to
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, solution build, packaged build, and 138
tests.
2026-06-08 local no-smoke verification passed after Manage Presets delete
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after Manage Presets delete
replacement-session encapsulation with `Verify.ps1 -SkipSmoke
-DisableNuGetAudit`: XAML accessibility lint, XAML localization lint, WinUI
code guards, solution build, packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after localized-surface refresh
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after top-modal close dispatch
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after monitor source-selection
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after Preset save-completion
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after Settings save-request
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after disconnected-monitor edit
result prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, solution build,
packaged build, and 138 tests.
2026-06-08 local no-smoke verification passed after Apply exception UI-state
prefactor with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after hard-coded status/progress
code guard with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.
2026-06-08 local no-smoke verification passed after raw enum fallback cleanup
and guard with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, solution build, packaged
build, and 138 tests.

2026-06-08 source-path policy tightened: Image sources now share the picker
extension allow-list in Core path normalization, so manually entered full paths
with unsupported extensions are rejected before becoming session or Preset state.
Validation copy now has English and Spanish messages for unsupported image file
types. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, solution build,
packaged build, and 139 tests.

2026-06-08 path validation prefactor: `WallpaperSourcePath.TryNormalizeImagePath`
now has an overload that returns the same typed `WallpaperSourcePathException`
error codes used by throwing normalization. This keeps future source-editor and
Preset repair flows from reconstructing validation reasons. Verified with
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, solution build, packaged build, and 140
tests with 0 warnings.

2026-06-08 Apply result prefactor: skipped-count propagation now goes through
`ApplySessionResult.WithSkipped`, including cancellation partial results, so
ready-source preflight accounting is not duplicated in `WallpaperApplyService`
success and catch branches. Verified with `Verify.ps1 -SkipSmoke
-DisableNuGetAudit`: XAML accessibility lint, XAML localization lint, WinUI code
guards, solution build, packaged build, and 141 tests with 0 warnings.

2026-06-08 picker selection validation: image picker results now pass through
`ImageSelectionDraft` and shared source-path validation before creating a
`MonitorSourceSelection`. Invalid picker paths now show localized validation
copy and do not mutate the active monitor assignment or overwrite the error with
"selected" status. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`:
XAML accessibility lint, XAML localization lint, WinUI code guards, solution
build, packaged build, and 141 tests with 0 warnings.

2026-06-08 renderer placement guard: `ImagePlacementPlan` now validates source
and target dimensions before fit/anchor/offset math, so invalid renderer inputs
fail with clear parameter errors instead of divide-by-zero or nonsensical draw
plans. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, solution build,
packaged build, and 142 tests with 0 warnings.

2026-06-08 Windows apply order prefactor: raw `IDesktopWallpaper` writes now go
through `DesktopWallpaperInterop.SetWallpaperThenPosition`, setting the rendered
wallpaper before forcing global Windows position to `Fill`. This avoids leaving
only a global position change behind if the wallpaper call fails before Windows
accepts the rendered path. Verified with `Verify.ps1 -SkipSmoke
-DisableNuGetAudit`: XAML accessibility lint, XAML localization lint, WinUI code
guards, solution build, packaged build, and 143 tests with 0 warnings.

2026-06-08 local JSON read prefactor: Preset and Settings reads now share
`LocalJsonFile.ReadAsync`, matching existing shared writes and keeping
source-generated JSON metadata in one storage helper. Store-specific load
policies still own fallback/normalization. Verified with `Verify.ps1 -SkipSmoke
-DisableNuGetAudit`: XAML accessibility lint, XAML localization lint, WinUI code
guards, solution build, packaged build, and 143 tests with 0 warnings.

2026-06-08 JSON guardrail: `Verify.ps1` now runs `TestJsonCodeGuards.ps1`,
which blocks direct `JsonSerializer` persistence calls outside `LocalJsonFile`.
Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, JSON code guards, solution
build, packaged build, and 143 tests with 0 warnings.

2026-06-08 PresetStore directory helper prefactor: Preset directory creation
now goes through `EnsurePresetsDirectory`, and preset file discovery uses a
named JSON search pattern. This keeps local Preset path policy in one place
before adding more app-managed Preset maintenance operations. Verified with
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, JSON code guards, solution build, packaged
build, and 143 tests with 0 warnings.

2026-06-08 MainPage text-presenter grouping prefactor: `MainPageTextPresenters`
now owns construction of Apply, Preset, monitor-edit, and shell text presenters.
`MainPageViewModel` still consumes the same focused presenters, but creation
has one entrypoint before command/surface split work continues. Verified with
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, JSON code guards, solution build, packaged
build, and 143 tests with 0 warnings.

2026-06-08 MainPage presenter field collapse: `MainPageViewModel` now stores one
`MainPageTextPresenters` dependency and exposes private aliases for existing
command code, removing four separate presenter fields without changing command
behavior. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, JSON code guards,
solution build, packaged build, and 143 tests with 0 warnings.

2026-06-08 Settings save request/store boundary prefactor:
`SettingsPreferenceStore.SaveRequestAsync` now accepts `SettingsSaveRequest`, so
`MainPageViewModel.SaveSettings` no longer reaches into request internals before
persisting Settings. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`:
XAML accessibility lint, XAML localization lint, WinUI code guards, JSON code
guards, solution build, packaged build, and 143 tests with 0 warnings.

2026-06-08 Settings save request encapsulation: `SettingsSaveRequest` now owns
applying its draft to loaded Settings and exposing its saved visual-memory id.
`SettingsPreferenceStore.SaveRequestAsync` consumes that request directly, so no
external caller reaches into request internals or persists raw Settings drafts.
Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, JSON code guards, solution
build, packaged build, and 143 tests with 0 warnings.

2026-06-08 monitor-key set factory prefactor: `MonitorKeys.CreateSet` now has
single-key and enumerable overloads, and Apply target/preflight code uses them
instead of constructing case-insensitive `HashSet<string>` instances manually.
Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, JSON code guards, solution
build, packaged build, and 144 tests with 0 warnings.

2026-06-08 Preset duplicate factory prefactor: duplicate Preset construction now
goes through `PresetFactory.Duplicate`, so `PresetStore` stays focused on local
JSON persistence while identity/name/timestamp creation stays with other Preset
construction rules. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`:
XAML accessibility lint, XAML localization lint, WinUI code guards, JSON code
guards, solution build, packaged build, and 145 tests with 0 warnings.

2026-06-08 Preset rename factory prefactor: rename Preset construction now goes
through `PresetFactory.Rename`, so `PresetStore` no longer owns name mutation
policy before saving. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`:
XAML accessibility lint, XAML localization lint, WinUI code guards, JSON code
guards, solution build, packaged build, and 146 tests with 0 warnings.

2026-06-08 Preset save normalization policy prefactor: `PresetFilePolicy` now
owns save-time schema/name/assignment/timestamp normalization through
`NormalizeForSave`, so `PresetStore.SaveAsync` stays focused on directory/path
and JSON writes. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, JSON code guards,
solution build, packaged build, and 147 tests with 0 warnings.

2026-06-08 rendered-directory helper prefactor: rendered output directory
creation now goes through `EnsureRenderedDirectory`, keeping folder policy in
one place before future rendered-output maintenance work. Verified with
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, JSON code guards, solution build, packaged
build, and 147 tests with 0 warnings.

2026-06-08 Apply result projection prefactor: `ApplyRunTracker` now creates the
final `ApplySessionResult` from current monitor state and counters, so
`WallpaperApplyService` no longer duplicates success and cancellation result
construction. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, JSON code guards,
solution build, packaged build, and 148 tests with 0 warnings.

2026-06-08 Apply cancellation skipped-count helper:
`ApplyCanceledException.WithSkipped` now owns adding ready-preflight skipped
counts to partial cancellation results, keeping `WallpaperApplyService`
catch-sites from manually rebuilding exception/result pairs. Verified with
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, JSON code guards, solution build, packaged
build, and 149 tests with 0 warnings.

2026-06-08 Settings save-result UI projection prefactor:
`SettingsPreferenceSaveResult` now owns save-status text selection and saved
visual-memory projection, so `MainPageViewModel.SaveSettings` no longer maps
write-failure/success states itself after persistence. Verified with
`Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML
localization lint, WinUI code guards, JSON code guards, solution build, packaged
build, and 149 tests with 0 warnings.

2026-06-08 Preset result success-helper prefactor:
`PresetSessionSaveResult.TryGetPreset` and
`ManagedPresetMutationResult.TryGetValue` now own success payload projection, so
Save, Save as, Rename, and Duplicate command handlers no longer inspect nullable
result payload shape directly. Verified with `Verify.ps1 -SkipSmoke
-DisableNuGetAudit`: XAML accessibility lint, XAML localization lint, WinUI code
guards, JSON code guards, solution build, packaged build, and 149 tests with 0
warnings.

2026-06-08 managed Preset delete-result helper:
`ManagedPresetDeleteResult.TryGetSuccessfulReplacement` now owns successful
delete replacement projection, so Confirm delete no longer checks write-failure
state directly before applying replacement session state. Verified with
`Verify.ps1 -DisableNuGetAudit`: XAML accessibility lint, XAML localization
lint, WinUI code guards, JSON code guards, solution build, tests, packaged
launch smoke, and 149 tests with 0 warnings.

2026-06-08 Manage Presets command-input prefactor:
`ManagedPresetCommandInput` now owns selected-Preset id lookup, required rename
name validation, duplicate input, and delete confirmation projection for Manage
Presets commands. `MainPageViewModel` applies returned status text instead of
keeping separate selection/name helper methods. Verified with `Verify.ps1
-SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML localization lint,
WinUI code guards, JSON code guards, solution build, packaged build, and 149
tests with 0 warnings.

2026-06-08 Preset required-name status prefactor:
`PresetNameInput` now has a status-aware required-name validation overload used
by Save as and Manage Presets input projection, so `MainPageViewModel` no longer
owns name-required status mapping. Verified with `Verify.ps1 -SkipSmoke
-DisableNuGetAudit`: XAML accessibility lint, XAML localization lint, WinUI code
guards, JSON code guards, solution build, packaged build, and 149 tests with 0
warnings.

2026-06-08 monitor assignment result-status prefactor:
`MonitorAssignmentUpdateResult` now owns updated-session projection and editor
status text selection for missing image paths, invalid edit values, and pending
changes. `MainPageViewModel.UpdateSelectedAssignment` now applies the result
instead of reading individual outcome flags. Verified with `Verify.ps1
-SkipSmoke -DisableNuGetAudit`: XAML accessibility lint, XAML localization lint,
WinUI code guards, JSON code guards, solution build, packaged build, and 149
tests with 0 warnings.

2026-06-08 selected-Preset load-result projection prefactor:
`SelectedPresetLoadResult` now owns selection projection, missing-Preset state,
and localized status text selection for Current setup, loaded Preset, and
missing Preset outcomes. `MainPageViewModel.LoadSelectedPresetAsync` no longer
switches on result kind to choose status copy or nullable selection state.
Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML accessibility
lint, XAML localization lint, WinUI code guards, JSON code guards, solution
build, packaged build, and 149 tests with 0 warnings.

2026-06-08 selected-Preset stale-list refresh policy prefactor:
`SelectedPresetLoadResult.ShouldRefreshPresetList` now owns whether a dropdown
load outcome should refresh stale Preset menu state, so
`MainPageViewModel.LoadSelectedPresetAsync` no longer checks for missing-Preset
kind directly. Verified with `Verify.ps1 -SkipSmoke -DisableNuGetAudit`: XAML
accessibility lint, XAML localization lint, WinUI code guards, JSON code guards,
solution build, packaged build, and 149 tests with 0 warnings.

2026-06-08 refreshed development MSIX package gate:
`Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit` passed after the latest
native prefactors. It covered XAML accessibility lint, XAML localization lint,
WinUI code guards, JSON code guards, solution build, packaged debug build,
tests, Release build, signed dev MSIX creation, and MSIX inspection. Output:
`artifacts\packages\Waller-dev-x64.msix`, size `78,687,150` bytes. Identity
`1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`, publisher `CN=Waller`, version
`1.0.0.0`, architecture `x64`; signature remains
`UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17` until the dev cert is
trusted. Tests: 149 passed, 0 failed, 0 skipped, 0 warnings.

2026-06-08 install preflight hardening: `InstallDevMsix.ps1` now runs the
current-user development package registration preflight before certificate
trust/install checks, blocking install when the same dev identity is already
registered unless `-SkipRegistrationCheck` is explicitly passed. Local preflight
found current-user registration
`1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_1.0.0.0_x64__yq0fg95n1tr90` under the
debug `AppX` output folder, then blocked before cert trust/install as expected.

2026-06-08 all-user install preflight option:
`InstallDevMsix.ps1 -AllUsersRegistrationCheck` now forwards to
`TestDevPackageRegistration.ps1 -AllUsers` and blocks install if that check is
inconclusive. Local run reported all-user access denied, also found the existing
current-user debug package registration, then blocked before cert trust/install;
no install or certificate trust mutation occurred.

2026-06-08 package asset/identity lint:
`TestPackageAssets.ps1` now verifies package display/publisher identity,
`VisualElements` display/description, manifest logo/splash/tile asset
references, matching project Content includes, and MainWindow icon usage.
`Verify.ps1` runs this lint before build/test/package gates so package identity
or asset regressions fail early.

2026-06-08 package version/identity guard:
`TestPackageAssets.ps1` now also verifies non-empty non-template package
identity name and MSIX four-part version range, so manual manifest edits cannot
bypass `SetPackageVersion.ps1` validation before package/build gates.
Package manifest path/version helpers now live in `PackageManifest.ps1` and are
shared by package lint, version editing, package inspection, registration
preflight, and uninstall preflight scripts, reducing drift between manifest
checks. `Verify.ps1` now also runs `TestPackageScriptGuards.ps1`, which blocks
new direct package-manifest reads outside the shared helper path.
`PackageManifest.ps1` now also owns reading `AppxManifest.xml` from MSIX
artifacts. `InstallDevMsix.ps1` uses that packaged identity for registration
preflight, so install checks cannot drift from the actual package being
installed. `UninstallDevPackage.ps1` can also read the identity from a supplied
MSIX `-PackagePath`, so cleanup preflight can target the same artifact as
install preflight without copying package names by hand.
`TestDevPackageRegistration.ps1` now also accepts `-PackagePath`, and
`InstallDevMsix.ps1` delegates registration checks through that path instead of
extracting and forwarding package names manually.
Package identity resolution now lives in `Get-WallerPackageIdentity`, shared by
registration and uninstall preflights for explicit package names, MSIX artifacts,
and source manifests.

2026-06-08 packaged launch smoke refresh:
`scripts\SmokeLaunch.ps1 -DisableNuGetAudit` built and launched the packaged
debug app successfully. `winapp` returned AUMID
`1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`, process
`Waller.Native.App` opened with main window title `Waller`, responded, and was
closed by the smoke script.

2026-06-08 current development MSIX package gate:
`Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit` passed after the latest
Core Apply and package-script prefactors. It covered lints, solution build,
packaged debug build, 151 tests, Release build, signed dev MSIX creation, and
MSIX inspection. Output: `artifacts\packages\Waller-dev-x64.msix`, size
`78,687,092` bytes, identity `1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4`, publisher
`CN=Waller`, version `1.0.0.0`, architecture `x64`; signature remains
`UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17` until the dev cert is
trusted.

2026-06-08 dev certificate trust preflight:
`TestDevCertificateTrust.ps1` now resolves both `winapp.exe` and the dev PFX to
absolute paths before printing the elevated trust command. Local preflight still
reports the dev certificate as not trusted, but now prints a copy/paste-safe
command:
`C:\Users\cristian\.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.3.2\tools\win-x64\winapp.exe cert install D:\DEV\waller\native\artifacts\signing\devcert.pfx`.
`PrepareDevCertificate.ps1` and `PackageDevMsix.ps1` now print the same
absolute PFX path style, so every cert/package path leads to the same elevated
trust command.

2026-06-08 full local verify gate:
`Verify.ps1 -DisableNuGetAudit` passed after the cert/package command cleanup.
It covered lints, solution build, 151 tests, packaged debug build, launch smoke,
process verification, and cleanup. `winapp` returned AUMID
`1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4_yq0fg95n1tr90!App`, process
`Waller.Native.App`, window title `Waller`, and `Responding=True`.

2026-06-08 Apply target-plan no-target prefactor:
`ApplyTargetPlan` now has an explicit `None` plan and `ReadyKeys` returns it
when preflight produces no ready monitor keys. `Matching` also rejects null
predicates immediately. This keeps no-target Apply behavior explicit before
future selected/batch workflows.

2026-06-08 Apply preflight-result factory prefactor:
`ApplyPreflightResult` now owns no-target, single-ready-target, and set-based
result construction. `ApplyPreflight` uses those factories instead of rebuilding
empty and case-insensitive monitor-key sets at call sites.

2026-06-08 Apply result error-code guard:
`ApplyResult.Failure` now normalizes unknown or missing error codes to
`wallpaper-apply-failed`, while preserving known codes. This keeps Apply
service/session/UI boundaries on the same small error-code vocabulary before
final localization and modal copy work.

2026-06-08 Apply cancellation projection prefactor:
`ApplyRunTracker` now creates `ApplyCanceledException` from current partial
monitor state, so `WallpaperApplyService` no longer owns cancellation
result/exception construction at each cancel site.

2026-06-08 Apply step-result prefactor:
`MonitorApplyStepResult` now owns success/failure monitor projection and
`ApplyRunTracker.Record` owns success/failure counter updates from a step
result. `WallpaperApplyService` now applies the step outcome without duplicating
counter branching or raw step-result construction.

Release trimming is disabled for now. First Release build exposed trim warnings
from `System.Text.Json` reflection-based serialization and manual COM
activation. JSON stores now use source-generated metadata through
`WallerJsonContext`; COM activation now has an explicit trim-analysis
suppression because `IDesktopWallpaper` is activated by CLSID, not by a managed
constructor. A manual `PublishTrimmed=true` probe now leaves warnings only in
external Windows SDK/WinRT assemblies. Keep Release untrimmed until packaged
trimmed launch/apply is manually validated.

## Good Next Commit Shape

Small useful next commit after this docs/checklist slice:

```text
native: run manual smoke and fix first blocker
```

Contains:

- real launch/apply notes from `docs\TESTING.md`
- first fix for any blocker found in packaged app usage

Avoid combining with packaging. Real smoke findings should stay focused.
