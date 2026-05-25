---
description: "Use when editing the React/Tauri frontend, Wallpaper Session state, monitor cards, preview loading, profile composition, or editor flows. Covers the main seams under src/."
name: "Frontend Wallpaper Session"
applyTo: "src/**/*.{ts,tsx}"
---
# Frontend wallpaper-session rules

- Use the domain terms from `src/CONTEXT.md` in new symbols, tests, and docs.
- Route UI workflows through `useWallpaperSession` and `wallpaperSession` instead of duplicating orchestration in `App.tsx` or components.
- Keep pure rules in `src/lib/` modules; keep components as presentational as practical.
- Preserve `src/lib/tauri.ts` as the only frontend IPC seam.
- If you change markers (`__NONE__`, `__SOLID__:*`), fit modes, profile limits, or preview semantics, update tests and matching Rust code/docs in the same task.
- For errors shown in UI flows, normalize them through `appErrors` instead of throwing raw payloads around.
