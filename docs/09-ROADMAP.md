# Technical Roadmap

## Already completed recently

- React/TypeScript migration and modularization away from the old monolithic flow
- Dedicated Wallpaper Session / Profile Composition / Preview Registry seams
- Built-in lightweight editor
- Persistent logging and log rotation
- `health_check` backend command
- Shared project rules/skills inside `.github/`
- Tauri JS/Rust dependency-alignment guard

## Short term

- Add UI-level interaction tests for `App`, `MonitorCard`, and `EditorDialog`.
- Surface `health_check` in the UI for support and diagnostics.
- Improve monitor-card feedback after a successful or failed single-monitor apply.
- Add profile export/import for easier portability between setups.

## Medium term

- Add real Tauri/WebView smoke tests for critical end-to-end flows.
- Introduce recent configuration history / restore points.
- Improve preview/cache lifecycle management for larger monitor setups.
- Harden release packaging with additional validation around produced artifacts.

## Long term

- Optional dynamic wallpaper/plugin architecture behind a deliberate feature boundary.
- Installer/update improvements once release cadence justifies the extra complexity.
- Advanced profile management features (grouping, tagging, maybe profile metadata) without compromising the current simple core flow.
