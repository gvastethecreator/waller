# Waller Native Implementation Plan

This plan turns the native architecture into small implementation slices.

Use this as the working order unless new discoveries change risk.

## Slice 0: Base Shell

Status: implemented initial version.

Goal:

- solution exists
- app builds
- app launches
- packaged Waller shell appears
- edit panel updates Active Session in memory

Done:

- solution/projects created
- Core models created
- first Core tests created
- WinUI page replaced with Waller shell
- packaged build and launch smoke scripts created
- standard verify gate created

Acceptance:

- `dotnet build .\Waller.Native.slnx` succeeds - done through `Verify.ps1`
- `dotnet test .\Waller.Native.Tests\Waller.Native.Tests.csproj` succeeds -
  done through `Verify.ps1`
- `BuildAndRun.ps1 ... -SkipRun` succeeds - done through
  `Verify.ps1 -SkipSmoke`
- launching through `BuildAndRun.ps1` opens Waller shell - done through
  `SmokeLaunch.ps1` local runs

## Slice 1: Real Monitor Detection

Status: implemented initial version.

Goal:

- replace sample data with current Windows monitor data
- keep app read-only against Windows wallpaper state

Tasks:

- implement `WindowsMonitorDetector` - done
- enumerate monitor device paths through `IDesktopWallpaper` - done
- read monitor rectangles - done
- read current wallpaper path where available - done
- preserve negative coordinates - done through Windows rect mapping
- map missing current wallpaper to `WallpaperSource.Empty` - done
- keep `SampleMonitorDetector` for tests/dev fallback - done
- use `EmptyMonitorDetector` as production fallback instead of sample monitor
  data - done
- isolate startup primary detection and fallback loading in separate helpers -
  done
- optional future: add `Microsoft.Windows.CsWin32`

Acceptance:

- app lists real connected monitors
- monitor bounds match Windows topology
- current wallpaper paths show when available
- no Apply path touches Windows yet
- header Refresh command reloads current Windows state without restart - done
- tests cover exact key and fallback matching

Risks:

- packaged COM behavior
- monitor IDs changing across docks
- path may be empty/null for Windows defaults or unsupported state

## Slice 2: Preset Store UI

Status: implemented initial version. Local JSON storage, dropdown load, Save,
Save as, Manage Presets modal, rename, duplicate, and delete confirmation exist.
Saved/unsaved row labels, Preset status copy, and recoverable local-data
write/read failures have friendly localized messages.

Goal:

- make local Presets real before apply/render work gets complicated

Tasks:

- add Preset dropdown view model - done
- list Presets from `%LOCALAPPDATA%\Waller\presets` - done
- implement Save - done
- implement Save as modal - done
- implement Manage Presets modal - done
- implement Rename - done
- implement Duplicate - done
- implement Delete with confirmation - done
- disable Manage Presets mutation controls while delete confirmation is open -
  done
- capture and show the delete-confirmation target Preset - done
- handle no Presets state - done through Current setup item
- show no Presets state inside Manage Presets - done
- reject blank Preset menu item names before picker/list surfaces render them -
  done
- guard Preset save/load/active-rename session DTOs before Preset view-model
  split - done
- guard Preset load/delete/mutation result shapes before command handlers
  consume them - done
- guard Manage Presets command inputs, delete confirmations, selection helpers,
  and Preset menu ids before modal commands mutate local Presets - done
- guard Preset menu list/refresh/localized-surface helpers against missing
  collections, blank Current setup labels, empty selection ids, and stale
  visual-memory result shapes before dropdown selection changes - done
- validate blank rename and missing/corrupt rename target - done
- mark dirty state in header and rows - done initial version
- skip corrupt local Preset JSON during list/load - done

Acceptance:

- Save as creates JSON - done
- dropdown can load saved Preset into Active Session - done
- Save updates selected Preset - done
- Rename/Duplicate/Delete work - done
- selecting Preset does not apply wallpaper - done
- startup still creates Active Session from current Windows state - done
- corrupt Preset JSON does not block the app - done

Risks:

- unclear dirty state if user edits after selecting Preset
- missing monitor assignments must be preserved
- disconnected Preset assignments are visible in the UI
- disconnected Preset assignments can be forgotten before saving
- disconnected Preset assignments can be reassigned to a current monitor before
  saving

## Slice 3: File Picker and Source UX

Status: implemented initial version. File picker works, missing-source warning
is visible, unsupported image file types are rejected before session mutation,
and missing image sources block affected Apply operations before Windows is
touched.

Goal:

- replace placeholder image picker and source fields with real UX

Tasks:

- implement WinUI file picker adapter - done
- initialize picker with app HWND - done
- restrict to image extensions - done
- show selected image path - done
- require Image source paths to be full local paths - done
- reject unsupported Image source file types through shared Core path policy -
  done
- add missing source detection - done
- add simple color picker or validated hex entry - done, native ColorPicker
  plus validated hex and quick swatches
- validate quick-swatch DTO colors/brushes before editor source selection uses
  them - done
- polish Empty source state - done, placement controls are disabled when the
  selected source is Empty or SolidColor because fit/anchor/offset only affect
  image rendering
- add source-specific visibility states - done
- keep Image source selected while path is empty, without mutating assignment -
  done
- normalize source-picker DTO paths/colors before editor fields mutate session
  state - done

Acceptance:

- Choose image opens native picker
- selected path becomes `WallpaperSource.Image`
- invalid/missing path blocks Apply for that monitor - done
- missing-source helper rejects missing source objects before UI/preflight code
  can turn caller bugs into null-reference failures - done
- unsupported image extension is rejected before it becomes session state - done
- color validates `#RRGGBB` - done, Core validation plus `ColorHexTextBox`
  format hint/length guard
- Empty source remains valid
- Image source can be selected before choosing a path

Risks:

- file picker requires `InitializeWithWindow`
- image extension filtering must not over-restrict valid formats needed later

## Slice 4: Renderer

Status: implemented initial version. Empty, SolidColor, and Image sources render
to final PNG. Image rendering uses Windows codecs and supports MVP placement
modes.

Goal:

- render final PNG per monitor before Windows apply

Tasks:

- choose rendering library/approach for Image source - done, Windows codecs
- implement `WallpaperRenderer` - done
- implement `Image` source load - done
- implement `SolidColor` - done
- implement `Empty` - done
- implement fit modes:
  - Cover - done
  - Contain - done
  - Stretch - done
  - Center - done
  - Tile - done
- implement 3x3 anchor math - done
- write files through `RenderedWallpaperStore` - done
- add renderer tests for geometry - done
- add at least one temp-file integration test - done

Acceptance:

- PNG output dimensions equal monitor bounds
- Empty renders black
- SolidColor renders exact color
- Image source renders for each fit mode
- anchor affects crop/placement predictably
- render happens only on Apply command path

Risks:

- selecting rendering library too early
- DPI confusion: output should use pixel bounds, not effective UI pixels
- large images may use significant memory

Renderer decision:

- use Windows codecs through `Windows.Graphics.Imaging` for MVP image decode
- keep output PNG writer in Core
- avoid adding Win2D/SkiaSharp until a real image-quality or performance issue
  appears
- guard PixelBuffer ownership and coordinate bounds before more image placement
  work builds on it
- rendered-wallpaper output paths are required to be absolute before Apply
  receives them

## Slice 5: Apply Pipeline

Status: implemented initial version. UI calls render + applier for Apply
monitor/all. Supported sources are Empty, SolidColor, and Image.

Goal:

- apply rendered PNGs to Windows per monitor

Tasks:

- implement `DesktopWallpaperApplier` - done
- call Windows wallpaper API with rendered PNG path - done
- apply one monitor - done
- apply all monitors - done
- block concurrent Apply operations from UI and command handler - done
- set per-monitor status:
  - Applying - internal service transition
  - Applied - done
  - Error - done
- support partial failure - done
- clear Apply progress and show localized status on unexpected pipeline failure
  - done
- add Cancel Apply command during active Apply - done
- keep no automatic rollback
- localize/app-map errors later - done for stable Apply error categories

Acceptance:

- Apply monitor changes only target monitor where Windows supports it
- Apply all renders and applies all current monitors
- Apply monitor/all skips missing image sources through Core preflight and
  reports the skipped count while applying ready monitors
- Apply all with no current monitors returns an empty result without rendering
  or touching Windows - done in Core coverage
- Apply ready-source paths with no ready monitors bypass the render/apply loop
  and emit no progress events - done in Core coverage
- no-op and skipped-only Apply result construction lives on
  `ApplySessionResult` - done
- Apply result rejects missing session references before UI/cancellation
  projection - done
- Apply result counts reject negative succeeded/failed/skipped values,
  including skipped-count cloning - done
- Apply cancellation and step-result contracts reject missing result/monitor
  references and blank failure codes before tracker aggregation - done
- Apply step-result construction stays behind success/failure helpers so
  monitor state transitions cannot be bypassed before tracker aggregation - done
- Apply step-result failures normalize unknown error codes before monitor state
  mutation so tracker inputs stay on the small Apply error vocabulary - done
- ApplyResult rejects missing monitors, hides direct construction behind
  factory helpers, and normalizes unknown failure codes for failures - done
- Apply error-code normalization is centralized in `ApplyErrorCodes.Normalize`
  so render exceptions, applier results, and apply service fallback copy share
  one error vocabulary - done
- Apply preflight result rejects missing session or key-set references before
  ready/skipped target selection - done
- Apply preflight result rejects overlapping ready/skipped monitor key sets
  before target selection - done
- Apply preflight rejects missing session and blank single-monitor keys before
  missing-source planning can turn invalid caller input into no-target output -
  done
- Apply target plans reject blank monitor keys, missing ready-key sets, and
  null monitor/list inputs before counting selected targets - done
- monitor-key set creation rejects blank/null key input and always returns
  case-insensitive sets - done
- monitor snapshots reject null identity/source and blank display names before
  row/progress/accessibility projection - done
- desktop wallpaper snapshots reject blank monitor ids and missing bounds before
  Windows detector projection - done
- monitor bounds reject non-positive dimensions before topology/render/UI code
  consumes geometry - done
- monitor session transition helpers reject null monitor/assignment inputs and
  blank Apply error codes - done
- image placement plans reject non-positive direct draw dimensions before
  renderer pixel loops consume placement DTOs - done
- monitor session construction rejects missing monitor/desired-assignment
  references before Apply/row/Preset code consumes session state - done
- Apply UI result state rejects success without an updated session and missing
  final status copy before command handlers update the surface - done
- monitor assignment update result state rejects mixed success/error outcomes
  and missing editor/session/monitor-key dependencies before editor field
  changes mutate Active Session - done
- editor draft/source/disconnected-monitor result DTOs reject invalid source/fit/
  anchor enums, non-finite offsets, blank status text, and missing helper
  dependencies before monitor editor state reaches XAML or session mutation -
  done
- active-session factory/editor entrypoints reject missing session, monitor-key,
  source, placement, detector, and preset inputs - done
- Preset name/factory entrypoints reject null name, session, identity, and preset
  inputs before local JSON payload construction - done
- Preset model construction rejects missing names, assignment collections, saved
  monitors, sources, and placements before store/matcher/factory code uses them
  - done
- Preset matcher and assignment normalization reject null session/preset or
  assignment inputs before Apply-Preset matching - done
- Preset matcher assignment index rejects missing normalized assignment
  collections before exact/fallback matching consumes the index - done
- Preset store/save policy rejects blank or relative local-data roots and null
  presets before touching local JSON - done
- Active Session rejects null monitor/missing-assignment collections and copies
  incoming collections before later editor/preset/apply mutations - done
- Settings store/policy rejects blank or relative local-data roots and null
  settings before touching local JSON - done
- UserSettings converts null language values to an empty draft value before
  Settings normalization applies the default supported language - done
- Rendered wallpaper store rejects blank or relative local-data roots before
  creating cache output paths - done
- Rendered cache-clear results reject negative delete/failure counts before UI
  copy formats cache summaries - done
- Render requests/artifacts reject missing monitor/assignment/path data and
  invalid output dimensions before renderer/applier code uses them - done
- Renderer primitives reject missing pixel buffers/data and placement inputs
  before PNG/scaling work starts - done
- Wallpaper placement rejects invalid fit/anchor enum values before
  renderer/Preset matching code consumes placement modes - done
- Desktop wallpaper applier rejects missing writer/wallpaper inputs before file
  checks or COM calls - done
- Apply service rejects missing renderer/applier dependencies before Apply
  orchestration starts - done
- Apply service rejects missing sessions across monitor, ready-source, all, and
  matching entrypoints before target planning or progress tracking starts - done
- Atomic local file writes reject blank paths and missing write callbacks before
  creating temp files - done
- Apply progress counts reject negative completed/total values and completed
  greater than total - done
- Apply progress rejects missing or blank monitor names before UI projection -
  done
- Apply run tracker rejects negative target totals before progress/result
  accounting starts - done
- Apply run tracker rejects missing progress monitors, step results, sessions,
  and monitor lists before progress or result projection - done
- failed monitor does not block success status for previous monitors - done
- Windows is never touched if render fails before apply - done
- status remains visible after operation - done through persistent
  `StatusInfoBar` plus XAML guard

Risks:

- Windows may apply behavior globally if using wrong API/position
- packaged permissions/runtime identity
- monitor key mismatch between detector and applier - partially mitigated:
  direct Apply monitor key matching is case-insensitive

## Slice 6: Settings

Status: implemented initial version. Settings modal exists for theme, language,
clear rendered cache, and window size/position persistence. Last selected
Preset visual memory remains.

Goal:

- app preferences become real

Tasks:

- implement Settings modal - done
- theme: System / Light / Dark - done
- language: English / Spanish - persisted and applied to main UI labels
- clear rendered cache - done
- disable Settings app-data actions while Apply runs - done
- save window size/position - done
- restore window size/position - done
- remember last selected Preset visually only - done
- fall back to defaults when settings JSON is corrupt - done
- normalize unsupported theme/language/window-size settings - done
- reject unsupported App-side Settings DTO theme/language values, impossible
  failed-save result shapes, and missing Settings store/request/draft/status
  dependencies before modal save/load mutates local state - done

Acceptance:

- theme persists - done
- language persists - done
- cache clear removes rendered files - done
- window size/position persists - done
- last selected Preset does not auto-load on startup - done
- startup status text uses saved language before first session message - done

Risks:

- localization should not spread string logic into Core
- window placement with multi-monitor coordinates needs care

## Slice 7: Localization

Status: implemented lightweight version. The app uses a typed
`LocalizedText` object exposed by `MainPageViewModel.Text`. This keeps early
iteration simple without `.resw` resource overhead.

Goal:

- English and Spanish UI strings

Tasks:

- choose `.resw` or lightweight localizer - done, lightweight localizer
- move UI strings out of XAML/VM - partial, main visible MVP labels done
- translate MVP strings - partial, main visible MVP labels done
- localize saved/unsaved and missing-source row labels - done
- localize main Preset/Settings/Apply status messages - done
- localize Apply progress status from Core enum - done
- keep placement fit/anchor/offset copy in the localized catalog and project it
  through `PlacementText` - done
- split English and Spanish catalog values into language-specific files - done
- use named arguments for language catalog values - done
- map common row-level Apply errors to friendly localized summaries - done
- add language setting - done
- test runtime language switching or restart-required behavior - done by
  binding `Text` and raising property changes

Acceptance:

- main visible MVP labels have English and Spanish
- missing source warning prefix is localizable
- saved/unsaved row labels are localizable
- remaining raw exception/error strings are reviewed before MVP - initial common
  Preset, Settings, validation, and Apply paths done; Core/App guard now blocks
  new raw `Exception.Message` usage and interpolated `ApplyResult.Failure`
  messages in production code; unknown Apply error codes now use localized
  Apply-specific fallback copy instead of generic validation copy
- core monitor/source models have explicit constructors and guard coverage so
  invalid monitor keys/source kinds do not slip into future session/apply work
- Active Session collection boundaries reject null monitor/session/missing
  assignment items before editor, Preset matching, or Apply code consumes them
- Preset assignment collection boundaries reject null assignment items before
  matching/save normalization consumes them
- shared `RequiredList` helper keeps collection copy/null-item contracts
  consistent across Core models
- runtime Settings preference mutation validates supported theme/language before
  App save state mutates, while JSON-loaded invalid settings still normalize
- runtime window placement mutation clamps minimum size before Settings save
  state mutates
- Preset, Settings, and rendered-cache stores share
  `LocalDataRootDirectory.RequireFullyQualified` so local MVP state cannot drift
  into process-relative paths
- core Apply status carriers reject invalid `MonitorApplyStatus` values before
  row state, progress reporting, or Apply orchestration splits consume them
- Apply run accounting is guarded so success/failure recording cannot exceed the
  planned monitor target count
- no Core UI text leaks into final user messages

Risks:

- lightweight localizer may become unwieldy as strings grow
- enum value fallbacks now avoid raw names in current status/source/placement
  projection; manual smoke should still check for any remaining rare visible
  copy gaps
- remaining rare copy gaps still need a final manual smoke copy pass, but
  unknown Apply row errors no longer surface generic validation fallback copy

## Slice 8: UI Polish and Accessibility

Goal:

- make shell feel like a small native utility

Tasks:

- use icons for commands where appropriate - primary shell and monitor row
  actions, image picking, Preset management, and Settings actions now use
  native icon+text treatment where helpful; XAML lint blocks new `Button`
  `Content` shortcuts so actions stay explicit and accessible; repeated button
  icon/text content now goes through `Controls/IconText.xaml`, backed by shared
  XAML resources for sizing and spacing
- add tooltips for buttons - done; XAML lint now blocks buttons without
  `ToolTipService.ToolTip`
- add Settings tooltip/accessibility label - done
- add primary shell command tooltips/accessibility labels - done
- use compact, stable sizing - header command row now uses bounded horizontal
  scrolling instead of forcing the top bar wider than the window
- make monitor/work columns scrollable - done
- improve topology strip - done, compact tiles hide resolution text while
  retaining full accessible topology summaries
- scale topology strip from real monitor bounds - done
- guard topology layout inputs so null bounds or non-positive surface/tile
  dimensions fail before WinUI projection - done
- guard topology DTO direct construction and `with` updates so callers cannot
  bypass layout/tile dimension invariants - done
- highlight selected topology tile - done
- add keyboard focus order - initial tab order and command accelerators done
- add edit panel and modal tab order - done; XAML lint now blocks flow modal
  interactive controls without explicit, valid, unique `TabIndex`
- keep modal surfaces responsive at narrow widths - done, modal borders use
  `MaxWidth` plus stretch/margin and lint blocks fixed overlay width regressions
- move focus into Manage Presets and Settings on open - done
- keep modal focus routing in named code-behind handlers - done; WinUI guard
  blocks inline MainPage `PropertyChanged` handlers
- guard modal keyboard contract so Escape closes the top modal and modal opens
  focus the first useful control - done through `TestModalKeyboardContract.ps1`
- add keyboard accelerators for primary shell commands - done; shell command
  contract guard now locks top-shell command bindings, enablement gates, shortcut
  keys, and narrow-window horizontal overflow behavior
- add accessible names - done, interactive controls with automation ids now
  require explicit localized `AutomationProperties.Name` values through XAML
  lint
- add modal action accessible names - done
- use compact icon-only close buttons for modal dismissal - done, accessible
  names/tooltips still use localized Close text
- add loading/progress state - done for Apply
- add empty-state text for no monitors/no Presets - done through
  monitor-workspace and Manage Presets empty text
- add no-monitors empty state - done
- hide topology strip in no-monitors state - done
- add missing monitor section - done
- add forget/remove behavior for disconnected assignments - done
- add reassign behavior for disconnected assignments - done
- add missing source state - done
- add lightweight source previews in monitor rows - done, current and
  disconnected rows now share `Controls/SourcePreview.xaml`
- expose source-preview meaning to assistive tech - done; XAML lint blocks
  unnamed `SourcePreview` surfaces
- extract current-monitor row visual/actions from `MainPage.xaml` - done through
  `Controls/MonitorRow.xaml`
- extract disconnected-monitor row visual/actions from `MainPage.xaml` - done
  through `Controls/MissingMonitorRow.xaml`
- extract topology strip from `MainPage.xaml` - done through
  `Controls/TopologyStrip.xaml`
- extract monitor workspace from `MainPage.xaml` - done through
  `Controls/MonitorWorkspace.xaml`
- extract top shell header/toolbar from `MainPage.xaml` - done through
  `Controls/ShellHeader.xaml`
- extract Save As modal from `MainPage.xaml` - done through
  `Controls/SaveAsModal.xaml`
- extract Manage Presets modal from `MainPage.xaml` - done through
  `Controls/ManagePresetsModal.xaml`
- extract Settings modal from `MainPage.xaml` - done through
  `Controls/SettingsModal.xaml`
- extract selected-monitor edit panel from `MainPage.xaml` - done through
  `Controls/EditPanel.xaml`
- extract status/progress footer from `MainPage.xaml` - done through
  `Controls/StatusFooter.xaml`
- split selected-monitor source picking/color editor commands from the main
  editor partial - done through `MainPageViewModel.Editor.Source.cs`
- split selected-monitor placement reset/offset helpers from the main editor
  partial - done through `MainPageViewModel.Editor.Placement.cs`
- split remaining selected-monitor editor selection, assignment,
  disconnected-monitor, and option-refresh flow by responsibility - done
  through focused `MainPageViewModel.Editor.*.cs` partials
- split source-generated property-change hooks by workflow - done through
  focused `MainPageViewModel.Changes.*.cs` partials
- split observable state/collections by workflow - done through focused
  `MainPageViewModel.State.*.cs` partials
- split derived surface projections by workflow - done through focused
  `MainPageViewModel.Surface.*.cs` partials
- split Manage Presets modal commands by responsibility - done through focused
  `MainPageViewModel.PresetManagement.*.cs` partials
- split Preset save/load/selection flow by responsibility - done through
  focused `MainPageViewModel.Presets.*.cs` partials
- map common validation and monitor detection fallback errors to friendly copy - done
- localize Settings theme/language option display values - done
- localize edit panel source/fit/anchor option display values - done
- reject blank option display names before localized dropdowns render them -
  done
- reject missing option collections and null option entries before localized
  dropdown refresh reaches Settings/editor surfaces - done
- validate localized option refresh inputs and selected enum values before
  Settings/editor dropdown option projection reaches XAML - done
- localize monitor row and disconnected monitor placement summaries - done
- guard monitor row projection inputs and topology dimensions before monitor
  workspace row/topology state updates - done
- guard against hard-coded placement fit/anchor/offset labels in
  `PlacementText.cs` - done
- guard against moving English/Spanish static catalogs back into
  `LocalizedText.Catalog.cs` - done
- guard against unnamed English/Spanish catalog arguments - done
- guard localized surface refresh output as an explicit result object before
  Settings language changes update cached Preset/row labels - done
- refresh editor fields on monitor selection without mutating Active Session - done
- add detailed manual smoke checklist for launch, topology, previews, Presets,
  Settings, disconnected monitors, Apply, placement, and accessibility - done
- expand XAML accessibility/localization gates from `MainPage.xaml` to the full
  app XAML tree - done, with MainPage-only composition guards kept scoped
- deduplicate WinUI code-guard text-contract scanning through
  `Test-TextContracts` so future App DTO guard additions stay cheap - done

Acceptance:

- no layout jumps on monitor selection - improved by keeping source-specific
  editor controls inside a fixed-height `SourceEditorHost` scroll region, so
  Image/Color/Empty selection changes do not move placement controls
- text fits at common window sizes - improved through compact missing-monitor
  row actions plus topology/empty-state/status XAML guards
- keyboard-only editing works - improved through explicit edit-panel tab order
  for source, source details, and placement controls, plus a modal keyboard
  contract guard for Escape close and initial modal focus, plus a top-shell
  command contract guard for primary shortcuts; manual smoke still needed
- screen reader labels are meaningful - initial linted coverage done for
  controls, monitor rows, topology tiles, and footer status/progress surfaces;
  InfoBar warning/status surfaces and source previews now also expose names;
  row action buttons announce the target monitor instead of only generic
  actions; manual packaged smoke still needed
- app remains minimal, not dashboard-heavy
- manual smoke notes identify the next real blocker before packaging work

## Slice 9: Packaging and Release Readiness

Goal:

- prepare real user-installable package later

Tasks:

- decide packaging strategy - initial: unsigned release build first, signed
  MSIX later through `winapp package --cert`
- app icon/identity update - partial: display name, description, publisher,
  and app assets set for prototype
- signing strategy - done, dev signing stays local/ignored/manual-trust while
  release signing requires production cert + timestamping before public package
  handoff
- versioning - initial script done for read/update of MSIX four-part manifest
  version
- install/uninstall behavior - initial guarded scripts done; package installs
  stay isolated in `InstallDevMsix.ps1` and removals stay isolated in
  `UninstallDevPackage.ps1`
- keep Presets/settings/rendered cache under one update-stable local app-data
  root - done through `WallerAppDataPaths` plus code guard
- reject blank App-side local-data roots before composing the Waller app folder -
  done through `WallerAppDataPaths.RootFor(...)` plus WinUI code guard
- reject missing App-side services/stores before composing startup view-model
  flows - done through `WallerAppServices`, `WallerLocalDataStores`, and
  `MainPageViewModel` constructor guards plus WinUI code guard
- guard app-data path policy so package identity changes cannot move
  Presets/settings/rendered cache away from `%LOCALAPPDATA%\Waller` - done
- guard launch contract so AUMID suffix, window title, and smoke launch stay
  aligned with package identity - done
- keep current-session detection/fallback policy out of App view-model files -
  done through Core `CurrentSessionLoader` tests plus WinUI code guard
- guard update policy so version bumps cannot change package identity or move
  Presets/settings away from `%LOCALAPPDATA%\Waller` - done
- add JSON source-generation metadata for Presets and Settings - done
- add explicit trim-analysis suppression for manual `IDesktopWallpaper` COM
  activation - done
- keep Release trimming disabled until external WinRT trim warnings are resolved
  or trimmed packaged launch/apply is manually validated - done
- guard signing policy so cert artifacts remain ignored/local and docs keep dev
  signing separate from release signing - done
- centralize package-registration conflict guidance so `0x80073D19` smoke
  blockers print the same read-only diagnostics and intentional-cleanup warning
  from smoke, registration preflight, and uninstall preflight - done
- smoke test on clean Windows user profile

Acceptance:

- clean package installs
- app launches from Start menu - covered at launch-contract level by stable
  `Application Id`, Waller window title, and `winapp` smoke script guard; real
  Start-menu smoke still required after package registration conflict is cleared
- app data path correct - done through `WallerAppDataPaths`,
  `WallerLocalDataStores`, and `TestLocalDataPolicy.ps1`
- app updates do not erase Presets/settings - covered at policy/script level by
  stable package identity versioning plus `%LOCALAPPDATA%\Waller` data root;
  real update smoke still required before release

## "Do Not Build Yet" List

Avoid until MVP proves core:

- image editor
- Identify overlay
- logs UI
- import/export
- dynamic/plugin wallpapers
- legacy Tauri profile import
- tray behavior
- scheduled wallpaper changes

Guard:

- `scripts\TestMvpScopeGuards.ps1` blocks feature hooks for this list from
  App/Core while the MVP proves core launch/apply/save behavior.
- span mode
- manual crop/zoom/offset UI

These may return later, but they are not first-native-app work.
