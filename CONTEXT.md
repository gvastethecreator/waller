# Waller Product Language

Waller is a Windows desktop app for preparing and applying one wallpaper assignment per monitor. These terms are canonical in product copy, code, tests, and active documentation.

## Workspace

**Monitor**:
A display reported by Windows and shown in the workspace. A disconnected monitor is a previously known display that is not currently available.
_Avoid_: Screen, display slot

**Active Session**:
The editable set of monitor assignments currently open in Waller. It can differ from both Windows and the last saved Preset until the user applies or saves it.
_Avoid_: Wallpaper Session, working profile

**Current Setup**:
The unsaved Active Session shown when no stored Preset is selected.
_Avoid_: Default profile, temporary preset

## Wallpaper configuration

**Wallpaper Source**:
The image, solid color, or empty source assigned to a Monitor.
_Avoid_: Asset, file value

**Monitor Assignment**:
The Wallpaper Source and placement choices prepared for one Monitor inside the Active Session.
_Avoid_: Wallpaper Draft, monitor config

**Placement**:
The fit, anchor, and position values used when rendering a Wallpaper Source for a Monitor.
_Avoid_: Transform settings, layout config

**Rendered Wallpaper**:
The PNG produced from a Monitor Assignment and made available to the Windows shell.
_Avoid_: Preview, cache image

## Saved work and actions

**Preset**:
A named local snapshot of monitor assignments that Waller can load into the Active Session.
_Avoid_: Profile, account

**Save**:
Persist the Active Session into the selected Preset. Save does not change the Windows wallpaper.
_Avoid_: Apply

**Apply**:
Render valid Monitor Assignments and ask Windows to use the resulting wallpapers. Apply does not save a Preset.
_Avoid_: Save, publish

**Preview**:
A non-persistent visual representation of a Wallpaper Source or Monitor Assignment inside Waller.
_Avoid_: Rendered Wallpaper
