# Maintenance & Stack Update — 2026-05-25

## Scope of this refresh

This maintenance pass focused on four goals:

1. Upgrade web and Rust dependencies to current versions.
2. Re-sync project documentation with the refactored architecture.
3. Add shared project rules and skills for future contributors/agents.
4. Address low-cost/high-value maintenance gaps discovered during the audit.

## Dependency snapshot after the refresh

### Web stack

- `react` / `react-dom` `19.2.6`
- `typescript` `6.0.3`
- `vite` `8.0.14`
- `@vitejs/plugin-react` `6.0.2`
- `tailwindcss` / `@tailwindcss/vite` `4.3.0`
- `vitest` / `@vitest/coverage-istanbul` `4.1.7`
- `jsdom` `29.1.1`
- `oxlint` `1.66.0`
- `@tauri-apps/api` `2.11.0`
- `@tauri-apps/plugin-dialog` `2.7.1`
- `@tauri-apps/plugin-log` `2.8.0`
- `@tauri-apps/cli` `2.11.2`

### Rust / Tauri stack

- `tauri` `2.11.2`
- `tauri-build` `2.6.2`
- `tauri-plugin-dialog` `2.7.1`
- `tauri-plugin-log` `2.8.0`
- `windows` `0.62.2`
- `serde` `1.0.228`
- `serde_json` `1.0.150`
- `dirs` `6.0.0`
- `base64` `0.22.1`
- `thiserror` `2.0.18`
- `log` `0.4.29`

## Improvements applied

### 1. Dependency alignment guard

- Added `scripts/check-tauri-version-sync.mjs`.
- Wired it into `bun run deps:tauri:check`.
- Included that check at the start of `bun run verify`.

This prevents silent drift between:

- `@tauri-apps/api`
- `@tauri-apps/cli`
- Cargo `tauri`
- JS/Rust plugin pairs (`dialog`, `log`)

`tauri-build` is still reported by the script for visibility, but it is not treated as a line-matching failure because its published version line does not currently mirror Cargo `tauri` exactly.

### 2. Backend compatibility fix for `windows 0.62`

- Updated `src-tauri/src/wallpaper.rs` to use the current `BOOL` type import expected by the newer `windows` crate.

### 3. Better seam-level coverage

- Added `src/lib/wallpaperSession.integration.test.ts`.
- The new test covers the flow:
  - refresh monitors
  - load profile
  - resolve preview
  - open/save editor
  - apply monitor
  - save profile

### 4. Shared project rules and skills

Added:

- `.github/copilot-instructions.md`
- `.github/instructions/frontend-wallpaper-session.instructions.md`
- `.github/instructions/rust-tauri.instructions.md`
- `.github/instructions/documentation-sync.instructions.md`
- `.github/skills/waller-maintenance/SKILL.md`
- `.github/skills/waller-wallpaper-session/SKILL.md`

### 5. Documentation sync

- Rewrote the stale architecture, implementation, testing, structure, UI, roadmap, and debt docs.
- Fixed the broken `docs/INDEX.md` reference to a missing `10-*` file by replacing it with this current maintenance note.
- Updated both PRDs to match the product as it actually exists today, including the editor and diagnostic flows.

## New maintenance commands

- `bun run deps:web:check`
- `bun run deps:web:update`
- `bun run deps:rust:update`
- `bun run deps:tauri:check`
- `bun run deps:update`

## Watch items after the refresh

- Keep an eye on TypeScript 6 and the Vite/React plugin line during future Tauri upgrades.
- Expand UI-level interaction coverage beyond the current store/domain-heavy tests.
- Consider exposing the backend `health_check` command in the UI for support workflows.