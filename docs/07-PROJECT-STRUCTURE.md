# Project Structure & File Purpose

## Summary tree

```text
waller/
├─ .github/
│  ├─ copilot-instructions.md
│  ├─ instructions/
│  │  ├─ documentation-sync.instructions.md
│  │  ├─ frontend-wallpaper-session.instructions.md
│  │  └─ rust-tauri.instructions.md
│  ├─ skills/
│  │  ├─ waller-maintenance/
│  │  │  └─ SKILL.md
│  │  └─ waller-wallpaper-session/
│  │     └─ SKILL.md
│  └─ workflows/
│     ├─ ci.yml
│     └─ release.yml
├─ .vscode/
│  ├─ settings.json
│  └─ tasks.json
├─ docs/
│  ├─ 01-PRD.md
│  ├─ 02-ARCHITECTURE.md
│  ├─ 03-SERVICES-AND-DEPENDENCIES.md
│  ├─ 04-IMPLEMENTATION.md
│  ├─ 05-AUDIT-AND-IMPROVEMENTS.md
│  ├─ 06-TESTING-AND-OPERATIONS.md
│  ├─ 07-PROJECT-STRUCTURE.md
│  ├─ 08-UI-DESIGN.md
│  ├─ 09-ROADMAP.md
│  ├─ 10-MAINTENANCE-AND-STACK-UPDATE-2026-05-25.md
│  ├─ 11-TECHNICAL-DEBT.md
│  └─ INDEX.md
├─ scripts/
│  └─ check-tauri-version-sync.mjs
├─ src/
│  ├─ components/
│  │  ├─ EditorDialog.tsx
│  │  ├─ MonitorCard.tsx
│  │  └─ MonitorLayout.tsx
│  ├─ hooks/
│  │  ├─ useElementSize.ts
│  │  └─ useWallpaperSession.ts
│  ├─ i18n/
│  │  ├─ en.ts
│  │  ├─ es.ts
│  │  ├─ index.tsx
│  │  └─ types.ts
│  ├─ lib/
│  │  ├─ appErrors.ts
│  │  ├─ profileComposition.ts
│  │  ├─ profileComposition.test.ts
│  │  ├─ previewRegistry.ts
│  │  ├─ previewRegistry.test.ts
│  │  ├─ tauri.ts
│  │  ├─ types.ts
│  │  ├─ wallpaper.test.ts
│  │  ├─ wallpaper.ts
│  │  ├─ wallpaperLayout.ts
│  │  ├─ wallpaperSession.integration.test.ts
│  │  ├─ wallpaperSession.test.ts
│  │  ├─ wallpaperSession.ts
│  │  ├─ wallpaperSessionState.ts
│  │  └─ wallpaperSource.ts
│  ├─ test/
│  │  └─ setup.ts
│  ├─ App.tsx
│  ├─ CONTEXT.md
│  ├─ identify.html
│  ├─ identify.ts
│  ├─ index.html
│  ├─ main.tsx
│  ├─ styles.css
│  └─ vite-env.d.ts
├─ src-tauri/
│  ├─ build.rs
│  ├─ Cargo.toml
│  ├─ capabilities/
│  │  └─ default.json
│  ├─ icons/
│  ├─ src/
│  │  ├─ error.rs
│  │  ├─ lib.rs
│  │  ├─ logger.rs
│  │  ├─ main.rs
│  │  ├─ profiles.rs
│  │  ├─ wallpaper.rs
│  │  └─ wallpaper_value.rs
│  └─ tauri.conf.json
├─ PRD.md
├─ README.md
├─ package.json
├─ tsconfig.json
├─ vite.config.ts
└─ vitest.config.ts
```

## Folder purpose

### `.github/`

- shared project instructions, skills, and GitHub workflows
- this is the repo-local automation/customization layer for contributors and coding agents

### `.vscode/`

- local development ergonomics such as runnable tasks

### `docs/`

- long-form project documentation for architecture, operations, roadmap, and maintenance

### `scripts/`

- small maintenance scripts that support the root workflow
- currently includes the Tauri JS/Rust version-alignment guard

### `src/`

- React/Vite frontend
- UI composition, grouped Wallpaper Session actions, i18n, and pure frontend domain logic

### `src/lib/`

The main frontend domain layer:

- `wallpaperSession.ts` — session orchestration
- `wallpaperSessionState.ts` — pure draft/baseline state ops
- `profileComposition.ts` — profile persistence rules
- `previewRegistry.ts` — preview request deduplication
- `wallpaperSource.ts` — marker and fit semantics
- `tauri.ts` — IPC bridge

### `src-tauri/`

- Rust/Tauri runtime, IPC command boundary, native Windows wallpaper logic, profile persistence, and persistent logging

## Project nature

Waller is intentionally:

- Windows-focused
- multi-monitor centric
- local-first
- conservative at the IPC/security boundary
- oriented toward traceability through tests and logs

It is not a cross-platform abstraction layer in its current form.

## Maturity snapshot

- Core wallpaper/profile/editor flows are implemented.
- Architecture is now deeper and more explicit around the Wallpaper Session seam.
- Verification is reproducible from the repo root.
- Documentation and maintenance guidance are part of the repository, not tribal knowledge.
