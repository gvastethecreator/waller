# Technical Debt

## Prioritized backlog

1. **Component interaction coverage**
   - Domain/store coverage is now solid, but direct UI interaction coverage is still lighter than ideal.
   - Priority targets: `App`, `MonitorCard`, `EditorDialog`, profile toolbar flows, and logs modal flows.

2. **Real Tauri/WebView smoke tests**
   - The repo still lacks a true end-to-end path over the packaged runtime or live WebView.
   - Critical candidates: identify overlay, preview loading, edited PNG save/apply, and profile persistence through the native boundary.

3. **Health-check visibility**
   - `health_check` exists in the backend, but users/support cannot invoke it directly from the UI yet.
   - Exposing it would improve diagnosis of COM/AppData/environment issues.

4. **Observability granularity**
   - Logging is unified and rotated, but there is no configurable verbosity strategy yet (for example, stricter release logging vs. richer debug logging).

5. **Profile portability**
   - Profiles are local-first and useful, but export/import workflows are still missing.
   - That limits migration between machines or backup/restore ergonomics.

6. **Preview/cache lifecycle tuning**
   - Preview management is explicit now, but larger monitor/image sets may eventually benefit from more aggressive eviction or memory-sensitive strategies.

7. **Partial-profile policy**
   - When a Profile omits active monitors, the current behavior preserves unmatched baseline drafts.
   - A future explicit policy could offer preserve/clear/inherit semantics.

8. **Session-state module growth**
   - `wallpaperSessionState.ts` is currently fine as a concentrated pure helper module, but if it keeps growing it may deserve an explicit namespace/object API for discoverability.

9. **UI polish and accessibility**
   - The product is already clear and usable, but micro-interactions, keyboard coverage, modal semantics, and contextual explanation around diagnostic mode can still improve.
