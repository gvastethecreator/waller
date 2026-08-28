# Waller certification notes

Copy and adapt this document immediately before Partner Center submission. Replace bracketed values and update the date.

## Notes for certification

**Notes date:** `[YYYY-MM-DD]`  
**Product:** Waller  
**Store ID:** `[STORE ID]`  
**Package version:** `[MAJOR.MINOR.BUILD.REVISION]`  
**Submission commit:** `[GIT SHA]`

Waller is a local-first Windows wallpaper manager for single-monitor and multi-monitor desktops. No Waller account or test credentials are required.

### Important tester notice

The **Apply** action changes the certification machine's current desktop wallpaper. Test on a machine or profile where this change is acceptable. Waller's automated Apply smoke captures the previous wallpaper paths and restores them in a `finally` path. A human certification session must record the starting wallpaper configuration before testing.

### Basic test path

1. Install the package.
2. Launch **Waller** from the Start menu.
3. Confirm connected monitors are detected.
4. Select one monitor.
5. Choose a local image or solid color.
6. Change placement between Cover, Contain, Stretch, Center, or Tile.
7. Use **Save** to create a preset. Saving does not change the desktop.
8. Use **Apply** to render and apply the active session. Applying changes Windows wallpaper assignments.
9. Open preset management and verify load, rename, duplicate, and delete behavior.
10. Confirm the English UI.

The app is fully testable with one monitor. A multi-monitor machine provides the most complete validation because each display can use an independent source and placement.

### File and data behavior

- Waller reads only local image files explicitly selected by the user and current wallpaper paths reported by Windows.
- Original selected images are not modified or deleted.
- Presets and settings are stored in package-local application data.
- Rendered PNG files are written under `%USERPROFILE%\.waller\rendered` so the Windows wallpaper shell can read them.
- Waller is not designed to upload images, paths, presets, monitor information, or settings.
- Core wallpaper functionality does not require network access.

### Save versus Apply

Save and Apply are intentionally independent:

- **Save** persists a reusable preset.
- **Apply** renders the current active session and changes Windows.

A tester can save, rename, duplicate, load, or delete presets without applying a wallpaper.

### Cancellation and errors

Apply reports progress and supports cancellation. Missing or inaccessible files produce a clear error without deleting the original file or corrupting saved presets.

### Update behavior

The Store build relies on Microsoft Store for package updates. Package name and publisher remain stable so package-local presets and settings survive updates.

### Support and privacy

- Privacy policy: `[PUBLIC PRIVACY POLICY URL]`
- Product website: `[PUBLIC PRODUCT URL]`
- Support: `[PUBLIC SUPPORT URL OR EMAIL]`

## Restricted capability: `runFullTrust`

Waller is a native WinUI desktop application. It requires `runFullTrust` to perform the following desktop operations:

1. Enumerate connected desktop monitors and read their geometry/topology.
2. Read current wallpaper paths reported by Windows.
3. Read local image files explicitly selected by the user.
4. Render per-monitor PNG files outside package-virtualized storage so the Windows shell can read them.
5. Write local presets and settings.
6. Call the Windows `IDesktopWallpaper` API to apply the selected wallpaper configuration.

Waller does not use `runFullTrust` to elevate silently, bypass Windows security boundaries, modify original user images, inspect unrelated documents, or transmit wallpaper content.

## Troubleshooting for reviewers

### Only one monitor is available

All primary functionality remains testable. Assign a local image or color, change placement, save a preset, and Apply. Multi-monitor behavior is an extension of the same workflow.

### A selected image cannot be opened

Use a normal local PNG or JPEG in a user-owned folder. Protected or unavailable paths produce an error rather than trigger elevation.

### The wallpaper appears unchanged

Confirm Apply completed, the selected monitor assignment is not empty, and the source file is readable. Windows can briefly cache the previous visual while the shell refreshes.

### Rendered files remain after uninstall or update

Rendered wallpaper files may be preserved under `%USERPROFILE%\.waller\rendered` so an applied desktop does not immediately lose its referenced image. Original user-selected images are never deleted by Waller.
