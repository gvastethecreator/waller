# Product Requirements Document (PRD)

## Waller — Multi-Monitor Wallpaper Manager

**Platform:** Windows 10/11 only  
**Stack:** Tauri 2 + Rust + React 19 + TypeScript 6 + Vite 8 + Bun + Windows `IDesktopWallpaper`

### 1. Objective

Build a lightweight Windows desktop application that lets users manage wallpapers per monitor with a fast, observable workflow centered on one editable **Wallpaper Session**.

### 2. Core user-facing capabilities

- **Monitor detection:** Discover connected displays through the Windows wallpaper API and show their relative layout.
- **Per-monitor Wallpaper Source:** Each monitor can use:
	- a local image,
	- a solid colour,
	- or no wallpaper.
- **Fit control:** Support the Windows wallpaper fit modes `Center`, `Tile`, `Stretch`, `Fit`, `Fill`, and `Span`.
- **Profile management:** Save, load, list, and delete reusable configurations persisted locally.
- **Preview experience:** Show a preview of image-based wallpaper sources before apply.
- **Identify overlay:** Help users map the real monitor to the monitor card shown by the app.
- **Lightweight editor:** Allow quick pan/zoom/rotate/filter/tint adjustments and save the result as an edited PNG before applying.
- **Diagnostics:** Surface logs and stable error messages so failures are visible instead of silent.

### 3. UX requirements

The application should remain single-window, direct, and low-friction.

- Primary actions: `Apply Configuration`, `Save Profile`, `Load Profile`, `Delete Profile`, `Identify`, `View Logs`.
- The monitor grid must clearly show monitor index, monitor name, preview state, fit mode, and whether a monitor has unapplied changes.
- The editor should stay focused on fast wallpaper preparation rather than on being a full image authoring suite.

### 4. Data model requirements

**Fit values** must map directly to Windows wallpaper positions:

- `Center`
- `Tile`
- `Stretch`
- `Fit`
- `Fill`
- `Span`

**Profile payload** must persist:

- `profileName`
- `monitors[]`
	- `monitorId`
	- `imagePath`
	- `fitMode`

The `imagePath` field also carries marker-backed wallpaper sources such as `__NONE__` and `__SOLID__:#RRGGBB`.

### 5. Architecture requirements

- **Frontend (React/Tauri WebView):** owns the UI, preview/editor interactions, and grouped Wallpaper Session actions.
- **IPC seam:** a typed frontend adapter sends commands to the backend through Tauri.
- **Backend (Rust/Tauri):** validates commands, orchestrates blocking work, integrates with Windows APIs, and persists local data under `%APPDATA%/WallpaperManager`.

### 6. Non-functional requirements

- **Fast feedback:** preview loading and apply operations should communicate progress and failures.
- **Stability:** blocking native work must not freeze the UI thread.
- **Maintainability:** domain language and validation rules should stay aligned between TypeScript and Rust.
- **Observability:** persistent logs should be available for support and debugging.

### 7. Out of scope

- Animated or video wallpapers
- Automatic wallpaper downloads from online catalogues/services
- macOS or Linux support
- Full-featured image-authoring workflows (layers, masks, export presets, batch editing)
