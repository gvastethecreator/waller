# Public Readiness Report

**Audit date:** 2026-06-02
**Auditor:** automated public-readiness pass on `waller` (renamed from `walls`)
**Branch under review:** `main` (commit `0e6a3b8` at start of audit)
**Scope:** full repository — source, docs, config, CI, metadata, history
**Overall status:** `Almost ready, minor issues remain` (one external manual task)

## Summary

Waller is a Windows-only Tauri 2 desktop app (Rust + React 19 + TypeScript 6 + Vite 8 +
Bun). The codebase is well organized, the domain language is consistent, the
verification suite passes cleanly, and CI/release workflows do not require
secrets. The local rename to `waller` had been applied to source files and
docs, but the Cargo/Tauri metadata, package metadata, release workflow, and
remote URL were not yet updated.

The audit applied the rename consistently across metadata, removed a large
personal `agent skill` folder that had been accidentally committed, removed a
Spanish temporary log and two personal `.vscode/settings.json` files that
contained an absolute Windows path, hardened the `.gitignore`, and added
`CONTRIBUTING.md` and `SECURITY.md`.

The only remaining manual task is on the GitHub side: rename the GitHub
repository from `walls` to `waller` and update the remote URL.

## Changes made in this pass

### Tracked files removed

- `.agents/` (125 files, ~640 KB) — generic Rust learning skills with no
  relation to the project. Recommended to be kept out of the public repo.
- `tmp/2026-03-24-puesta-a-punto.md` — Spanish temp log; the parent `tmp/`
  directory was already gitignored, so this file was an accidental `git add`.
- `.vscode/settings.json` (root) — personal editor color theme with an
  absolute Windows path (`d:\DEV\waller`) and a window title referencing the
  author's machine. Not project content.
- `src/.vscode/settings.json` — same as above for the `src/` workspace.

### Files updated (rename `walls` → `waller`)

- `src-tauri/tauri.conf.json` — `productName`, window `title`, `bundle.publisher`,
  `bundle.copyright`, and the app `identifier` are now `Waller` /
  `com.gvastethecreator.waller`.
- `src-tauri/Cargo.toml` — package `name` is `waller`, lib `name` is
  `waller_lib`. `src-tauri/src/main.rs` updated to call `waller_lib::run()`.
- `src-tauri/Cargo.lock` — regenerates on next build (visible as new entry
  `name = "waller"`).
- `package.json` — `name` is `waller`. Added `keywords`, `author`, `homepage`,
  `bugs`, and `repository` fields. `description` kept.
- `.github/workflows/release.yml` — release name, portable ZIP name, and
  binary path are now `Waller v…` / `waller-{ver}-windows-x64-portable.zip`
  / `src-tauri/target/release/waller.exe`.
- `LICENSE` — copyright holder is now `Waller Contributors`.
- `src/index.html` — browser title is `Waller`.
- `src/i18n/en.ts` — header title is `Waller`.
- `.vscode/tasks.json` — `taskkill` target is `waller.exe`.
- `docs/07-PROJECT-STRUCTURE.md` — removed the obsolete `settings.json` entry
  from the tree and the folder purpose description.

### Files added

- `CONTRIBUTING.md` — short contribution workflow aligned with the existing
  Wallpaper Session seam and the `bun run verify` contract.
- `SECURITY.md` — private vulnerability disclosure path and a description of
  the current Tauri security posture (CSP, capabilities, `withGlobalTauri`,
  IPC validation).
- `PUBLIC_READINESS_REPORT.md` — this file.

### Files updated (supporting docs)

- `docs/INDEX.md` — links to `CONTRIBUTING.md`, `SECURITY.md`, `README.md`,
  and this report.
- `.gitignore` — added `.vscode/settings.json`, `src/.vscode/settings.json`,
  `*.swp`, `*~`, and `.agents/`.

## Security review

### Tracked files

- `git grep -n -I -E "API_KEY|SECRET|TOKEN|PRIVATE|PASSWORD|PASS|AUTH|BEARER|JWT|DATABASE_URL|PRIVATE_KEY"`
  only matches:
  - Educational Rust learning material in (now removed) `.agents/skills/rust/`
    (e.g. JWT in `rust-router/examples/workflow.md`).
  - The word `AUTHORS` in `LICENSE` (standard MIT text).
  - A hash substring in `bun.lock` that contains `AUTH`.
  No actual secret values, credentials, or tokens are present.
- `localhost` appears only in `vite.config.ts`, `package.json`, `README.md`,
  and `src-tauri/tauri.conf.json` to describe the local Vite dev server and
  the Tauri IPC origin in the CSP. All of these are public-safe.
- Commit author / committer email is the GitHub `noreply` alias
  (`920957+gvastethecreator@users.noreply.github.com`). No personal email
  exposure.
- No `.env`, `.pem`, `.key`, `.pfx`, `.crt`, `.der`, or `secrets` files are
  present in the working tree or in any commit (`git rev-list --all --objects`
  is clean for these patterns).

### Git history

- 11 commits, all by the same author, all in May 2026.
- No large binaries, archives, or generated artifacts are present in history
  (`git rev-list --all --objects` returns no `.exe`, `.msi`, `.dll`, `.zip`,
  `.tar`, `.pdf`, `.psd`, `.sketch`, `.fig`, `.wasm`, or `.map` paths).
- The `tmp/2026-03-24-puesta-a-punto.md` file is the only mildly sensitive
  historical entry (a Spanish temp log). It does not contain secrets or
  private data, but it is now untracked.
- No history rewrite was performed. If the GitHub rename is later paired with
  a fresh public mirror, the rename commit and the cleanup commit are the
  only public history events worth a rebase or squashed mirror.

### Secret scanner availability

- `gitleaks`, `trufflehog`, and `detect-secrets` are not installed in this
  environment. The keyword scan above is the substitute.

### Assets

- `src-tauri/icons/*` are standard Tauri bundle icons generated for this
  project (PNG/ICO). No third-party icons, fonts, or media are tracked.
- No third-party datasets or copied code are present.

## Git history review

- **Recommendation:** keep history. There are no secrets, no leaked
  credentials, no proprietary blobs, and only one mildly off-brand temp log.
  The repo is small (11 commits) and clean enough to publish as-is once the
  remote is renamed.
- Optional alternative for a cleaner public mirror: squash into a single
  release commit on a `release/0.1.0` branch and publish from there. Not
  required.

## Validation results

Run with `bun` 1.3.x, Rust stable (`x86_64-pc-windows-msvc`), Node 25.5.0,
on Windows.

| Command | Status | Notes |
|---|---|---|
| `bun run deps:tauri:check` | OK | JS/Rust Tauri and plugin versions aligned |
| `bun run typecheck` (`tsc --noEmit`) | OK | No diagnostics |
| `bun run lint:frontend` (`oxlint . --deny warnings`) | OK | No warnings |
| `bun run test:frontend` (`vitest run --coverage`) | OK | 5 files, 18 tests passed; ~67 % statements coverage on the domain layer |
| `bun run test:rust` (`cargo test --lib`) | OK | 14 tests passed (recompiled cleanly as `waller` / `waller_lib`) |
| `bun run lint:backend` (`cargo clippy -- -D warnings`) | OK | No warnings |
| `bun run check:rust` (`cargo check`) | OK | Compiles |
| `bun run web:build` (`vite build`) | OK | `dist/index.html`, `dist/identify.html`, hashed assets |

`bun run verify` (which chains the above) passes end-to-end.

## Documentation status

- `README.md` — current, English, no private notes. Project name and
  branding now match `waller`.
- `docs/01-PRD.md` through `docs/11-TECHNICAL-DEBT.md` — consistent English
  with the Wallpaper Session vocabulary; no stale names.
- `PRD.md` (root) — kept as a top-level summary. Aligns with
  `docs/01-PRD.md` and uses the correct vocabulary.
- `src/CONTEXT.md` — authoritative domain vocabulary file. Unchanged.
- `CONTRIBUTING.md` — new; covers the local setup, the `bun run verify`
  loop, and the PR checklist.
- `SECURITY.md` — new; private disclosure path and current Tauri security
  posture.
- `PUBLIC_READINESS_REPORT.md` — this file; linked from `docs/INDEX.md`.

## License status

- `LICENSE` is MIT, updated to `Waller Contributors`. Year 2026 is correct
  for the current release window.
- `package.json` `license` field is `MIT`. The two match.
- No third-party code or assets require an additional license file at this
  time.

## Remaining manual tasks

1. **Rename the GitHub repository** from `walls` to `waller` on
   github.com (`gvastethecreator/walls` → `gvastethecreator/waller`), then
   update the local remote:
   ```sh
   git remote set-url origin https://github.com/gvastethecreator/waller.git
   git remote -v
   ```
2. **Update the GitHub repository description and topics** to match the
   README: `Multi-monitor wallpaper manager for Windows built with Tauri 2,
   React 19 and TypeScript.` Suggested topics: `wallpaper`, `windows`,
   `multi-monitor`, `tauri`, `rust`, `react`, `typescript`, `desktop-app`.
3. **Configure GitHub repository settings** (cannot be done from the CLI):
   - Enable security advisories (used by `SECURITY.md`).
   - Enable branch protection on `main` requiring CI to pass before merge.
4. **Optional: refresh `Cargo.lock`** after the rebrand if a pinned
   downstream consumer relies on the old `wallpaper-manager` package name
   (none expected; this is a fresh public launch).

## Final recommendation

**Publish after the GitHub rename and the remote URL update.** All
in-repo changes needed for a credible public release are committed to
`main`; `bun run verify` is green; the security review found no
sensitive data; the license and metadata are aligned. The four manual
tasks above are external to the repository and must be completed before
the first public push of this commit history.
