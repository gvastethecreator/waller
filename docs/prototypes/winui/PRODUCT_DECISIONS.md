# Waller Native Product Decisions

Date: 2026-06-04

This document captures the product decisions for the new Waller WinUI app. The app is not a pixel-for-pixel migration of the current Tauri UI. It is a fresh native Windows app that uses the current Waller behavior as reference.

## Product Direction

Waller Native is a minimal, native, Fluent Windows app for managing wallpapers across multiple monitors.

The product should feel closer to PowerToys and Windows Settings than to a web dashboard. It should be compact, calm, fast, and useful with several monitors connected.

Primary goals:

- Detect monitors.
- Show the current Windows wallpaper state.
- Let the user create an editable Active Session.
- Let the user apply wallpapers per monitor or all monitors.
- Let the user save local Presets.
- Keep the UI minimal and seamless.

Non-goals for the MVP:

- Image editor.
- Identify Overlay.
- Logs UI.
- Manual import/export.
- Dynamic/plugin wallpapers.
- Full legacy profile compatibility.
- Heavy preview cache.
- Sidebar-heavy dashboard layout.

## Naming

### Product and Project

| Concept | Decision |
|---|---|
| Product/UI name | Waller |
| Internal project name | Waller.Native |
| App style | Native Fluent, utility-focused |

### Saved Configuration

Use **Preset**, not Profile.

Reason:

- "Profile" can imply account, user identity, or portable JSON files.
- "Preset" better describes a local saved configuration inside the app.

Definition:

```text
Preset:
A local saved snapshot of an Active Session.
It contains monitor wallpaper assignments, placement settings, and monitor identity metadata.
It does not imply manual import/export.
```

UI labels:

| English | Spanish |
|---|---|
| Preset | Preset |
| Save | Guardar |
| Save as | Guardar como |
| Save preset | Guardar preset |
| Current setup | Configuración actual |
| Apply all | Aplicar todo |
| Apply monitor | Aplicar monitor |

## Core Concepts

### Active Session

The **Active Session** is the editable state currently shown by the app.

Startup behavior:

```text
Open app
-> detect monitors
-> read current wallpapers from Windows
-> create Active Session
-> do not apply anything
-> do not auto-load a Preset
```

Rules:

- The user can edit the Active Session without changing Windows.
- The user can apply the Active Session without saving a Preset.
- The user can save the Active Session as a Preset without applying it to Windows.
- Apply and Save are independent actions.

### Preset

A **Preset** is a local saved snapshot of an Active Session.

Rules:

- Presets are stored inside app data.
- The user does not manually load or export JSON.
- Presets start fresh in Waller Native; no compatibility with old Tauri profiles is required in the MVP.
- Presets are not autosaved when the user edits the Active Session.
- If an Active Session was loaded from a Preset and then edited, the Preset is marked as modified until saved.

Preset actions:

- Select Preset.
- Save.
- Save as.
- Rename.
- Duplicate.
- Delete.

No MVP action:

- New blank Preset.

Reason:

Presets should originate from the current Active Session or from duplicating an existing Preset. A blank Preset is ambiguous because monitor topology is discovered from hardware.

### Wallpaper Source

A **Wallpaper Source** can be:

| Source | Meaning |
|---|---|
| Image | A path to an image on disk |
| SolidColor | A hex color selected by the user |
| Empty | No user-selected image/color; renders as black |

The new app should not use legacy marker strings such as `__NONE__` or `__SOLID__:#RRGGBB` internally. Those markers belong only to the old Tauri/Rust app or to a future migration adapter if needed.

Suggested model:

```csharp
public enum WallpaperSourceKind
{
    Image,
    SolidColor,
    Empty
}

public sealed record WallpaperSource(
    WallpaperSourceKind Kind,
    string? ImagePath,
    string? ColorHex
);
```

### Empty Source

`Empty` means black output.

Rules:

- If Windows has no detectable wallpaper for a monitor, the Active Session shows `Empty`.
- If the user applies `Empty`, Waller renders a black PNG for that monitor and applies it.
- `Empty` does not mean "skip this monitor".
- `Empty` does not mean "restore Windows default".

### Missing Image Source

If a Preset or Active Session references an image path that no longer exists:

- Show `Missing source`.
- Show the path in truncated form.
- Offer `Choose image`.
- Applying that monitor fails without touching Windows for that monitor.
- The Preset is not modified automatically.

## Monitor Identity

Presets should match assignments to monitors using a stable monitor key plus fallback metadata.

Each assignment should store:

- monitor key from Windows when available
- display index
- device/name label
- width
- height
- x/y bounds

Apply/load matching order:

1. Match exact monitor key.
2. Fallback match by resolution and approximate position.
3. If no match, mark assignment as missing/disconnected.

### Missing Monitors

When a Preset has assignments for monitors that are not currently connected:

- Load the Preset partially.
- Apply only to matched monitors.
- Show missing assignments under "Monitors not connected".
- Do not delete missing assignments from the Preset automatically.
- If the user saves changes, preserve or update missing assignments deliberately according to the save flow.

### New Monitors

When a Preset is loaded and extra monitors are currently connected:

- New monitors keep their current Windows state in the Active Session.
- They are editable.
- They are not added to the Preset until the user saves changes.

## Apply and Save Semantics

### Apply

`Apply` changes Windows.

Types:

- Apply all.
- Apply monitor.

Rules:

- Apply does not save a Preset.
- Apply renders final PNG files before calling Windows wallpaper APIs.
- Apply all can partially succeed.
- No automatic rollback on partial failure.
- Status is tracked per monitor.

Monitor apply statuses:

```text
Clean
Pending
Applying
Applied
Error
Missing
```

### Save

`Save` persists a Preset.

Rules:

- Save does not change Windows.
- Save updates the selected Preset.
- Save as creates a new Preset from the current Active Session.
- Editing an Active Session does not autosave the selected Preset.

### Applied vs Saved

The UI should distinguish:

```text
Applied to Windows
Saved to Preset
```

Example:

- A monitor can be applied but unsaved.
- A Preset can be saved but not applied.
- A Preset can be modified while some monitors are already applied.

Header example:

```text
PresetName · Modified
```

Monitor row examples:

```text
Applied · Unsaved
Pending
Error
```

## Placement

Waller Native should provide real per-monitor placement by prerendering final wallpaper images before applying them.

This avoids relying on `IDesktopWallpaper.SetPosition` for per-monitor behavior, because Windows wallpaper position is effectively global.

### Fit Modes

MVP fit modes:

| Mode | Meaning |
|---|---|
| Cover | Fill monitor while preserving aspect ratio; crop overflow |
| Contain | Fit whole image inside monitor; preserve aspect ratio; black bands |
| Stretch | Fill monitor by distorting image |
| Center | Draw image at original/rendered size centered or anchored; black background |
| Tile | Repeat image to fill monitor |

Out of MVP:

- Span.
- Manual crop editor.
- Manual zoom slider.
- Rotation.
- Filters.

### Anchor

MVP position control:

```text
TopLeft    Top    TopRight
Left       Center Right
BottomLeft Bottom BottomRight
```

Anchor applies especially to:

- Cover cropping.
- Contain placement when image does not fill the monitor.
- Center placement.

Out of MVP:

- offset X/Y sliders
- manual zoom
- crop handles
- rotation

### Background Fill

Use black as the background fill for:

- Empty source.
- Contain bands.
- Center extra area.

## Rendering

Waller Native should render final wallpapers as PNG files.

MVP decisions:

- Output format: PNG for everything.
- Render final files only when Apply is invoked.
- Do not write final rendered files on every control change.
- Preview can be approximate and UI-only.

Rendered output flow:

```text
Wallpaper Source
-> Placement
-> Monitor size
-> Renderer
-> rendered PNG
-> Windows wallpaper apply
```

Rendered cache:

```text
%LOCALAPPDATA%/Waller/rendered/
```

Rules:

- The rendered cache is persistent.
- It is managed by the app.
- It is not exposed as a user-facing file workflow.
- Cleanup is manual in MVP through Settings.
- No automatic retention policy in MVP.

## Source Image Handling

For image sources, store the original file path.

Rules:

- Do not copy original images into app data in MVP.
- If the original file disappears, show Missing source.
- Rendered PNG output does not replace the source.
- The Preset continues to refer to the original source path.

## Preset Storage

Presets should be stored as local JSON managed by the app.

Storage root:

```text
%LOCALAPPDATA%/Waller
```

Initial structure:

```text
%LOCALAPPDATA%/Waller/
  presets/
  rendered/
  settings.json
```

Possible future structure:

```text
%LOCALAPPDATA%/Waller/
  cache/
  logs/
```

Preset JSON should be versioned:

```json
{
  "schemaVersion": 1,
  "id": "preset-id",
  "name": "Preset name",
  "assignments": []
}
```

## User Preferences

MVP preferences:

- window size
- window position
- theme
- language
- last selected Preset as visual memory only

Important rule:

The app may remember the last selected Preset visually, but it must not auto-load or auto-apply that Preset on startup.

Settings storage:

```text
%LOCALAPPDATA%/Waller/settings.json
```

## Internationalization

MVP should support:

- English
- Spanish

Recommended implementation:

- Prefer `.resw` if straightforward in WinUI.
- Use a simple localizer if `.resw` slows the first implementation slice.

The requirement is bilingual MVP; the implementation can stay simple.

## UI Direction

### Visual Style

Use native Fluent Design from scratch.

References:

- PowerToys for density and utility.
- Windows Settings for native controls and calm hierarchy.

Avoid:

- Web-dashboard feel.
- Sidebar-heavy CRM layout.
- Large hero sections.
- Decorative graphics.
- Overloaded monitor cards.

### Navigation

MVP is a single-screen app.

Structure:

```text
MainWindow
  Header command bar
  Monitor topology strip
  Monitor list
  Right edit panel
  Manage Presets modal
  Settings modal
```

No MVP:

- permanent preset sidebar
- left navigation
- multi-page navigation
- tab-heavy UI

### Header

Header should include:

- Waller title
- Preset dropdown
- Save
- Save as
- Apply all
- Settings

Preset dropdown:

- Current setup
- Saved Presets
- Manage Presets

### Preset Management

Presets should not use a permanent sidebar.

Use:

- dropdown in the header
- Manage Presets modal

Manage Presets modal actions:

- Rename
- Duplicate
- Delete

### Monitor Topology Strip

Include a compact monitor topology strip.

Purpose:

- Show physical layout.
- Select monitor.
- Highlight monitor being edited.
- Show missing Preset assignments when relevant.

This replaces the need for Identify Overlay in MVP.

### Monitor Rows

Use compact monitor rows/cards with mini preview.

Suggested row:

```text
[mini preview] Monitor 1  2560x1440
               Image: beach.png
               Cover · Center
               Applied · Unsaved
               [Edit] [Apply]
```

Rules:

- Prefer compact rows over large cards.
- Mini preview should be enough to recognize the wallpaper.
- Do not show every edit control inline.

### Monitor Edit Panel

Use a right-side panel for editing a monitor.

Behavior:

```text
Click Edit
-> right panel opens
-> panel shows selected monitor
-> changes update Active Session immediately
-> Windows is not touched until Apply
-> closing panel preserves pending changes
```

Panel controls:

- Source: Image / Color / Empty
- Choose image
- Color picker
- Fit mode
- 3x3 anchor grid
- Source status/missing path

### Feedback

Use inline feedback.

Apply all feedback:

- progress ring or subtle progress indicator
- text such as `Applying 2 of 3...`
- success message inline
- errors per monitor

No modal for success.

Use modal/dialog only for:

- destructive confirmation
- critical unrecoverable errors

Partial apply failure:

- no rollback
- successful monitors stay applied
- failed monitors show error
- Active Session remains intact

### Settings

Use a lightweight Settings modal.

MVP settings:

- Theme: System / Light / Dark
- Language: English / Spanish
- Clear rendered cache

Future settings:

- Open app data folder
- Reset preferences
- cache retention policy

## MVP Feature Set

Included:

- Detect monitors.
- Read current Windows wallpaper per monitor when possible.
- Create Active Session from current Windows state.
- Image / SolidColor / Empty source.
- Cover / Contain / Stretch / Center / Tile.
- 3x3 anchor.
- Per-monitor prerendered PNG output.
- Apply monitor.
- Apply all.
- Local Presets.
- Preset dropdown.
- Manage Presets modal.
- Missing monitor handling.
- Missing source handling.
- Basic visual preview in UI.
- English/Spanish.
- Theme preference.
- Settings modal.

Excluded:

- Legacy profile import.
- Manual JSON import/export.
- Image editor.
- Identify Overlay.
- Logs UI.
- Dynamic/plugin wallpapers.
- Auto-apply on startup.
- Auto-load Preset on startup.
- Cleanup automation.
- Span.
- Manual offset/zoom/rotation/filter controls.

## Open Product Questions

These are intentionally left for later:

- Should future Presets support tags or grouping?
- Should source image paths be optionally copied into app data?
- Should background fill become configurable?
- Should Span return as a virtual-desktop rendering mode?
- Should Waller support portable mode?
- Should Waller eventually support scheduled/dynamic wallpapers?
