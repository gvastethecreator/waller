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
- polish Empty source state - done, placement controls are disabled when the
  selected source is Empty or SolidColor because fit/anchor/offset only affect
  image rendering
- add source-specific visibility states - done
- keep Image source selected while path is empty, without mutating assignment -
  done

Acceptance:

- Choose image opens native picker
- selected path becomes `WallpaperSource.Image`
- invalid/missing path blocks Apply for that monitor - done
- unsupported image extension is rejected before it becomes session state - done
- color validates `#RRGGBB`
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
- write files through `RenderedWallpaperStore`
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
- failed monitor does not block success status for previous monitors
- Windows is never touched if render fails before apply
- status remains visible after operation

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
- map common row-level Apply errors to friendly localized summaries - done
- add language setting - done
- test runtime language switching or restart-required behavior - done by
  binding `Text` and raising property changes

Acceptance:

- main visible MVP labels have English and Spanish
- missing source warning prefix is localizable
- saved/unsaved row labels are localizable
- remaining raw exception/error strings are reviewed before MVP - initial common
  Preset, Settings, validation, and Apply paths done
- no Core UI text leaks into final user messages

Risks:

- lightweight localizer may become unwieldy as strings grow
- enum value fallbacks now avoid raw names in current status/source/placement
  projection; manual smoke should still check for any remaining rare visible
  copy gaps
- remaining rare exception/error strings still need a final manual smoke copy
  pass

## Slice 8: UI Polish and Accessibility

Goal:

- make shell feel like a small native utility

Tasks:

- use icons for commands where appropriate
- add tooltips for icon-only buttons
- add Settings tooltip/accessibility label - done
- add primary shell command tooltips/accessibility labels - done
- use compact, stable sizing
- make monitor/work columns scrollable - done
- improve topology strip
- scale topology strip from real monitor bounds - done
- highlight selected topology tile - done
- add keyboard focus order - initial tab order and command accelerators done
- add edit panel and modal tab order - done
- move focus into Manage Presets and Settings on open - done
- add keyboard accelerators for primary shell commands - done
- add accessible names
- add modal action accessible names - done
- add loading/progress state - done for Apply
- add empty-state text for no monitors/no Presets
- add no-monitors empty state - done
- hide topology strip in no-monitors state - done
- add missing monitor section - done
- add forget/remove behavior for disconnected assignments - done
- add reassign behavior for disconnected assignments - done
- add missing source state - done
- add lightweight source previews in monitor rows - done
- map common validation and monitor detection fallback errors to friendly copy - done
- localize Settings theme/language option display values - done
- localize edit panel source/fit/anchor option display values - done
- localize monitor row and disconnected monitor placement summaries - done
- refresh editor fields on monitor selection without mutating Active Session - done
- add detailed manual smoke checklist for launch, topology, previews, Presets,
  Settings, disconnected monitors, Apply, placement, and accessibility - done

Acceptance:

- no layout jumps on monitor selection
- text fits at common window sizes
- keyboard-only editing works - initial version done; manual smoke still needed
- screen reader labels are meaningful
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
- signing strategy
- versioning - initial script done for read/update of MSIX four-part manifest
  version
- install/uninstall behavior
- add JSON source-generation metadata for Presets and Settings - done
- add explicit trim-analysis suppression for manual `IDesktopWallpaper` COM
  activation - done
- keep Release trimming disabled until external WinRT trim warnings are resolved
  or trimmed packaged launch/apply is manually validated - done
- smoke test on clean Windows user profile

Acceptance:

- clean package installs
- app launches from Start menu
- app data path correct
- app updates do not erase Presets/settings

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
- span mode
- manual crop/zoom/offset UI

These may return later, but they are not first-native-app work.
