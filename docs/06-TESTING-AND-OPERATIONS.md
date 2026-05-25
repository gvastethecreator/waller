# Testing & Operations

## Automated coverage today

### Frontend tests (`bun run test:frontend`)

Current Vitest coverage is centered on the wallpaper domain and session seams:

- `src/lib/wallpaper.test.ts`
  - marker parsing/encoding
  - fit-mode normalization
  - layout helpers
- `src/lib/profileComposition.test.ts`
  - profile save payload composition
  - active-monitor projection
  - validation failures
- `src/lib/previewRegistry.test.ts`
  - preview cache state transitions and deduplication
- `src/lib/wallpaperSession.test.ts`
  - session snapshot behavior, grouped flows, editor behavior, profile save through the seam
- `src/lib/wallpaperSession.integration.test.ts`
  - refresh -> load profile -> preview -> edit -> apply -> save profile

Coverage is generated with Istanbul through Vitest.

### Backend tests (`bun run test:rust`)

Rust tests cover the value/validation and persistence core:

- `src-tauri/src/profiles.rs`
  - profile-name sanitization
  - profile validation
  - roundtrip save/load/list/delete behavior
- `src-tauri/src/wallpaper.rs`
  - fit mapping
  - marker resolution helpers
- `src-tauri/src/wallpaper_value.rs`
  - solid-colour BMP generation and fit validation
- `src-tauri/src/lib.rs`
  - MIME inference
  - PNG data URL parsing
  - command error serialization

## Core commands

Run everything from the repository root unless you are diagnosing a lower layer directly.

| Command | Purpose |
|---|---|
| `bun run dev` | Full Tauri + Vite development |
| `bun run web:dev` | Frontend-only development |
| `bun run typecheck` | TypeScript verification |
| `bun run lint:frontend` | Frontend linting |
| `bun run test:frontend` | Frontend tests + coverage |
| `bun run test:rust` | Rust library tests |
| `bun run lint:backend` | `cargo clippy -- -D warnings` |
| `bun run check:rust` | `cargo check` |
| `bun run deps:tauri:check` | Tauri JS/Rust version alignment check |
| `bun run verify` | Full project verification |
| `bun run build` | Tauri production build |

### Dependency maintenance helpers

- `bun run deps:web:check`
- `bun run deps:web:update`
- `bun run deps:rust:update`
- `bun run deps:update`

## Recommended verification flow

### For normal code changes

1. Run `bun run verify`.

### For dependency changes

1. Run `bun run deps:tauri:check`.
2. Run `bun run verify`.
3. If packaging is affected, also run `bun run build`.
4. If the NSIS bundle is enabled, keep `src-tauri/tauri.conf.json` on NSIS language names such as `English` (not locale-style names like `EnglishUS`) so the installer can resolve its bundled `.nlf` files.

### For backend-only diagnosis

- `cd src-tauri && cargo test --lib`
- `cd src-tauri && cargo clippy -- -D warnings`
- `cd src-tauri && cargo check`

## Diagnostic flow

1. Reproduce the issue in the UI.
2. Open **View Logs** from the profile/action bar.
3. Look for scopes such as:
   - `client:*`
   - `backend`
   - `runtime`
   - `ui:*`
4. Correlate the log sequence with the operation (`browse`, `preview`, `apply`, `editor`, `profiles`).
5. If the issue smells platform-specific, inspect `%APPDATA%/WallpaperManager/logs/app.log` directly.

## Suggested manual verification checklist

- Detect monitors and verify the layout matches the physical setup.
- Change the Wallpaper Source per monitor:
  - image -> apply
  - solid colour -> apply
  - none -> apply
- Save and load Profiles preserving `__SOLID__` and `__NONE__` markers.
- Open the editor, adjust an image, save it, and confirm the edited PNG is applied.
- Trigger the Identify Overlay on a multi-monitor setup.
- Re-open the logs modal and confirm recent actions are present.
- When relevant, confirm a packaged build launches correctly.

## VS Code tasks

Defined in `.vscode/tasks.json`:

- `wallpaper: tauri dev`
- `wallpaper: tauri build`
- `wallpaper: cargo check`
- `wallpaper: cargo test (lib)`
- `wallpaper: smoke tests`
- `wallpaper: test + check`
- `wallpaper: kill running app`
- `wallpaper: bun verify`
- `wallpaper: full verify`

## CI and release workflows

- `.github/workflows/ci.yml`
  - installs dependencies on `windows-latest`
  - runs `bun run verify`
- `.github/workflows/release.yml`
  - runs the same verification
  - builds the Tauri app
  - packages a portable ZIP and NSIS installer

## Expected outcome

- `bun run verify` must pass cleanly.
- `bun run build` must produce the Windows release artifacts under `src-tauri/target/release/`.
- The NSIS installer step must resolve a valid bundled language file (currently `English`).
- The frontend production bundle must be emitted to `dist/`.
