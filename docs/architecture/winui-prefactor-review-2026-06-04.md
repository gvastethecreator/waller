# Architecture Review - WinUI Prefactors

Date: 2026-06-04

## Summary

- `GITHUB_RESEARCH_REPORT.md` supports the current direction: WinUI 3 + C#/.NET, `CommunityToolkit.Mvvm`, CsWin32 for native Windows calls, no Rust dependency in the final app.
- The risky migration work is not the shell. The risky work is preserving **Wallpaper Source**, **Wallpaper Draft**, **Profile**, monitor geometry, and global fit-mode behavior across a new runtime.
- The current app already has useful seams, but some interfaces are still shaped around Tauri. Deepening those modules first will make the WinUI implementation easier.
- One prefactor is already applied: `WallpaperSessionRuntime` now lives in `src/lib/wallpaperRuntime.ts` instead of inside `wallpaperSession.ts`.

## Research Report Analysis

`GITHUB_RESEARCH_REPORT.md` is aligned with the migration report in `docs/prototypes/winui/MIGRATION_REPORT.md`.

Useful decisions from the report:

- Use `CommunityToolkit.Mvvm` directly.
- Use CsWin32 for `IDesktopWallpaper`, `EnumDisplayMonitors`, `GetSystemMetrics`, and related monitor functions.
- Keep Phase 1 narrow: real monitor detection, current wallpaper read, file picker, apply selected, apply all.
- Avoid Win2D, SkiaSharp, MagicScaler, ImageSharp, WinUIEx, Vanara, and DesktopManager in the first slice.
- Treat `IDesktopWallpaper.SetPosition` as a global fit-mode risk, not a per-monitor capability.

Report caveats:

- External repository freshness and license details should be verified again before adding dependencies.
- The report currently lives at `GITHUB_RESEARCH_REPORT.md` while its own suggested path is `docs/prototypes/winui/GITHUB_RESEARCH_REPORT.md`.
- Runtime setup notes in `docs/prototypes/winui/NOTES.md` may need updating now that the prototype launches on the machine.

## Recommendations

### 1. Deepen the Wallpaper runtime seam

**Recommendation strength**: Strong

**Files**

- `src/lib/wallpaperRuntime.ts`
- `src/lib/wallpaperSession.ts`
- `src/lib/tauri.ts`
- `src/hooks/useWallpaperSession.ts`
- `src/lib/wallpaperSession.test.ts`
- `src/lib/wallpaperSession.integration.test.ts`

**Problem**

The **Wallpaper Session** module exposed the runtime interface from inside its implementation file. That made the seam look like a store detail even though it is the exact interface a future WinUI adapter must satisfy.

**Solution**

Keep `WallpaperSessionRuntime` in its own module. Treat `tauriWallpaperSessionRuntime` as one adapter, and the future WinUI C# runtime module as another implementation of the same conceptual interface.

**Benefits**

- locality: runtime call contracts sit in one module instead of being embedded in session implementation.
- leverage: Tauri, tests, and WinUI planning can share one small interface.
- tests cross the runtime seam without importing store implementation types.

**Before / After**

Before: callers imported `WallpaperSessionRuntime` from `wallpaperSession.ts`, which also contains command queueing, snapshots, editor flow, profile flow, and preview flow.

After: callers import the runtime interface from `wallpaperRuntime.ts`; `wallpaperSession.ts` consumes it.

**Dependencies / sequencing**

- Applied now.
- Unblocks a future C# interface mapping document because the seam is explicit.

**Documentation follow-ups**

- Update `docs/02-ARCHITECTURE.md` to name `wallpaperRuntime.ts` as the runtime seam.
- Add a future WinUI implementation note that maps each runtime method to a C# module.

### 2. Extract shared Wallpaper contract fixtures

**Recommendation strength**: Strong

**Files**

- `src/lib/types.ts`
- `src/lib/wallpaperSource.ts`
- `src/lib/profileComposition.ts`
- `src-tauri/src/wallpaper_value.rs`
- `src-tauri/src/profiles.rs`
- proposed: `docs/contracts/wallpaper-contract.md`
- proposed: `docs/contracts/fixtures/*.json`

**Problem**

The **Wallpaper Source**, fit mode, and **Profile** contracts exist in TypeScript and Rust, but the contract itself is not captured as data. A WinUI rewrite could accidentally drift on markers, casing, JSON field names, or limits.

**Solution**

Create contract docs and fixture JSON files for:

- supported fit modes
- `__NONE__`
- `__SOLID__:#RRGGBB`
- image path sources
- valid profile payload
- invalid profile payloads
- active-monitor projection behavior

**Benefits**

- locality: cross-runtime compatibility is verified against one contract instead of rediscovered in TS/Rust/C#.
- leverage: fixtures can drive TypeScript tests now and C# tests later.
- interface shrinks: WinUI implementation only needs to satisfy documented inputs/outputs.

**Before / After**

Before: TypeScript normalizes sources, Rust resolves markers, and tests cover behavior inside each runtime.

After: the contract becomes a shared artifact; TypeScript, Rust, and C# implementations can all prove compatibility.

**Dependencies / sequencing**

- Do this before porting `ProfileService` or `WallpaperSource` to C#.
- It unlocks C# unit tests without needing a running WinUI app.

**Documentation follow-ups**

- Add `docs/contracts/wallpaper-contract.md`.
- Link from `docs/02-ARCHITECTURE.md`, `docs/prototypes/winui/MIGRATION_REPORT.md`, and `src/CONTEXT.md`.

### 3. Name global fit-mode semantics explicitly

**Recommendation strength**: Strong

**Files**

- `src/lib/wallpaperSessionState.ts`
- `src-tauri/src/wallpaper.rs`
- `docs/prototypes/winui/MIGRATION_REPORT.md`
- proposed: `docs/adr/0001-global-fit-mode.md`

**Problem**

The code already knows that fit mode behaves globally in Windows: `updateBaselineAfterSingleApply` updates every baseline fit mode after one monitor apply, and Rust calls `SetPosition` once for the desktop. The interface does not make this domain rule obvious.

**Solution**

Record an ADR that Waller currently treats fit mode as a global Windows wallpaper setting, even when **Wallpaper Draft** stores a fit mode per **Monitor** for profile compatibility and UI convenience.

**Benefits**

- locality: the surprising Windows behavior is captured once.
- leverage: WinUI implementation will not chase impossible independent per-monitor `SetPosition` behavior.
- tests can assert global fit-mode effects as intended behavior, not accidental state updates.

**Before / After**

Before: the global behavior is implicit in implementation details.

After: the global behavior is a named interface rule that C# modules must preserve or intentionally replace through pre-rendered monitor images.

**Dependencies / sequencing**

- Do this before the WinUI `WallpaperService.ApplyAsync` implementation.
- It unlocks a clean decision later: keep global fit mode or build pre-rendered per-monitor outputs.

**Documentation follow-ups**

- Create ADR.
- Add a short note to `src/CONTEXT.md` under flagged ambiguities.

### 4. Split Wallpaper Session command handling by domain flow

**Recommendation strength**: Worth exploring

**Files**

- `src/lib/wallpaperSession.ts`
- `src/lib/wallpaperSessionState.ts`
- `src/lib/profileComposition.ts`
- `src/lib/previewRegistry.ts`

**Problem**

`wallpaperSession.ts` is a deep module in behavior, but the implementation is large and mixes monitor refresh, draft updates, profile flow, preview flow, editor flow, identify flow, and logging. The interface is useful; the implementation has low locality for changes.

**Solution**

Keep the external `WallpaperSessionStore` interface stable, but move internal command implementations into small internal modules grouped by **Wallpaper Session** flow:

- monitor/draft commands
- profile commands
- preview commands
- editor commands
- identify commands

These should be internal implementation modules, not new external seams.

**Benefits**

- locality: each migration-relevant behavior can be inspected and ported independently.
- leverage: the store interface remains one test surface.
- implementation absorbs the complexity while preserving the useful external seam.

**Before / After**

Before: one switch statement owns every command.

After: one store coordinates state and queueing; flow modules own implementation details.

**Dependencies / sequencing**

- Do after shared contract fixtures.
- Do before porting the session state to C# if we want the port to follow the same flow groupings.

**Documentation follow-ups**

- Update `docs/02-ARCHITECTURE.md` only if the internal modules become stable enough to mention.

### 5. Deepen monitor geometry into a shared Monitor topology module

**Recommendation strength**: Worth exploring

**Files**

- `src/lib/wallpaperLayout.ts`
- `src-tauri/src/wallpaper.rs`
- `src/components/MonitorLayout.tsx`
- future WinUI monitor layout ViewModel

**Problem**

The report calls out negative coordinates, mixed DPI, and rectangle correlation between `IDesktopWallpaper`, GDI, and `DisplayArea`. The current frontend layout function handles virtual coordinates, but the contract is implicit and only UI-facing.

**Solution**

Create a **Monitor topology** module that owns:

- virtual bounds
- normalized layout coordinates
- negative-coordinate behavior
- rectangle equality/intersection helpers
- fixture cases for left/above-primary monitors

**Benefits**

- locality: monitor geometry math is concentrated before WinUI adds `DisplayArea` and `AppWindow`.
- leverage: one module supports current React layout, identify overlays, and WinUI layout.
- tests cover the interface that matters for multi-monitor correctness.

**Before / After**

Before: layout math is a pure helper used by React.

After: topology is a named module with fixtures that WinUI can port directly.

**Dependencies / sequencing**

- Do before implementing WinUI identify overlays.
- Can happen after Phase 1 if apply-one does not need overlay placement.

**Documentation follow-ups**

- Add **Monitor topology** to `src/CONTEXT.md` if accepted.

### 6. Separate editor work from the Wallpaper Session migration path

**Recommendation strength**: Strong

**Files**

- `src/components/EditorDialog.tsx`
- `src/lib/wallpaperSession.ts`
- `docs/prototypes/winui/MIGRATION_REPORT.md`
- `GITHUB_RESEARCH_REPORT.md`

**Problem**

The editor mixes browser image loading, canvas transforms, filters, tint, drag state, export, and immediate apply behavior. It is the highest-cost WinUI migration item and should not shape the first native app.

**Solution**

Define an **Editor output** contract: given a **Monitor**, source image path, fit mode, and adjustments, return a saved image path that can become a **Wallpaper Source**. Keep the first WinUI slice editor-free.

**Benefits**

- locality: editor complexity stops leaking into monitor/apply/profile migration.
- leverage: WinUI can implement apply-one/apply-all without image libraries.
- interface shrinks: future Win2D/SkiaSharp spike only needs to satisfy one output contract.

**Before / After**

Before: editor save is embedded in the **Wallpaper Session** command switch.

After: editor becomes a replaceable internal flow with a small output contract.

**Dependencies / sequencing**

- Document now.
- Implement after Phase 1 and Phase 2.

**Documentation follow-ups**

- Add an editor-specific spike note under `docs/prototypes/winui/`.

## Suggested execution order

1. **Deepen the Wallpaper runtime seam** - already applied; it makes the WinUI adapter shape visible.
2. **Extract shared Wallpaper contract fixtures** - strongest next prefactor because it protects compatibility before C# code exists.
3. **Name global fit-mode semantics explicitly** - prevents a false per-monitor-fit implementation path.
4. **Deepen monitor geometry into Monitor topology** - useful before identify overlays and DisplayArea work.
5. **Split Wallpaper Session command handling by domain flow** - worthwhile after contracts are stable; keep external store interface unchanged.
6. **Separate editor work from the Wallpaper Session migration path** - document now, implement later when the editor spike starts.

## Documentation fan-out

- `src/CONTEXT.md`: add or sharpen terms for **Monitor topology**, global fit mode, and **Editor output** if accepted.
- `docs/adr/0001-global-fit-mode.md`: record the global `SetPosition` decision.
- `docs/contracts/wallpaper-contract.md`: create the compatibility contract and fixtures.
- `docs/prototypes/winui/MIGRATION_REPORT.md`: link to this review and the contract docs once accepted.
