# Technical PRD — Waller

## Product goal

Provide a lightweight Windows desktop tool for preparing, previewing, editing, saving, and applying wallpapers per monitor through one coherent **Wallpaper Session**.

## Target audience

- Users with 2+ monitors on Windows 10/11
- Users who switch between work, gaming, streaming, or productivity setups
- Users who value explicit feedback and reproducible local profiles over cloud sync or online catalogues

## Functional scope

### Implemented capabilities

1. **Monitor detection**
   - Primary source: `IDesktopWallpaper`
   - Geometry and visualization support: GDI (`EnumDisplayMonitors`)
2. **Per-monitor Wallpaper Source**
   - Local image
   - Solid colour marker
   - No wallpaper marker
3. **Wallpaper fit**
   - `Center`, `Tile`, `Stretch`, `Fit`, `Fill`, `Span`
4. **Profiles**
   - Save, load, list, delete
   - Preserve wallpaper-source markers in persisted payloads
5. **Preview workflow**
   - Preview generation through backend data URLs
   - Deduplicated preview loading and error caching
6. **Editor workflow**
   - Quick pan/zoom/rotate/filter/tint adjustments
   - Save edited output as PNG and apply immediately
7. **Observability**
   - Persistent logs in `%APPDATA%/WallpaperManager/logs/app.log`
   - In-app log viewer and clear action
8. **Operational helpers**
   - Identify Overlay for monitor mapping
   - Backend `health_check` command for subsystem diagnostics

### Out of current scope

- Animated/video wallpapers
- Integration with online wallpaper catalogues
- macOS/Linux support
- Advanced image-authoring workflows with layers or export pipelines

## Non-functional requirements

- **Stability:** report failures through the UI and logs instead of failing silently.
- **Performance:** preview loading must be async and deduplicated.
- **Maintainability:** keep domain language and validation logic aligned between TypeScript and Rust.
- **Traceability:** changes should be verifiable through tests, scripts, and CI-friendly commands.

## Acceptance criteria (current status)

- [x] Detect active monitors and show their layout
- [x] Configure wallpaper per monitor
- [x] Apply solid colour per monitor
- [x] Clear wallpaper per monitor
- [x] Save, load, list, and delete profiles
- [x] Preview image-based wallpaper sources
- [x] Edit and save a prepared PNG from the built-in editor
- [x] View and clear execution logs
- [x] Run frontend and backend automated checks from the repository root
