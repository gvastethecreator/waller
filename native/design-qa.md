# Design QA: monitor composer

## Target

Selected visual: option 3 from the product-design review. The target establishes a light native desktop workspace with a persistent monitor rail, a central monitor stage, compact primary actions, and a direct editor below the stage.

## Native adaptation

- Waller uses the monitor assignment that is actually loaded. The rail and stage render a real image only when that assignment contains an existing image file; solid-color assignments keep their true color preview.
- The window starts at 1536 × 1024, matching the selected reference viewport, and centers in the active work area. The former 1120 × 760 default migrates once; custom saved placements remain intact.
- On smaller work areas, the initial size clamps to the available desktop while the rail and editor keep their scroll paths.
- The editor reacts to monitor selection from either the rail or the stage. The compact `Más` menu keeps save, preset, settings, and refresh actions reachable.

## Comparison and checks

### 2026-07-23 topology and theme pass

The stage now consumes each monitor's Windows virtual-desktop bounds directly.
The source order stays tied to the detected monitor identity, while the
custom topology panel arranges each tile at its scaled X/Y coordinate. This
keeps negative coordinates, unequal sizes, and the physical 2–1–3 layout
visible in the stage instead of flattening the monitors into a row.

The default theme is dark. Existing settings without an explicit theme choice
migrate to dark, and the Settings modal exposes a working dark/light toggle.
Preview badges now sit inside their preview surface with an inset margin, the
editor arrows use a row-major 3 × 3 layout, and the header action widths fit at
the current Windows display scale.

Evidence from the last valid three-monitor Windows run:

- Physical topology and dark stage: `.scratch/planning/2026-07-22-winui-recovery/evidence/ui-topology-absolute-fixed.png`
- Inset numeric badges and dark default: `.scratch/planning/2026-07-22-winui-recovery/evidence/ui-final-dark-topology-badge.png`
- Header Apply action after the width fix: `.scratch/planning/2026-07-22-winui-recovery/evidence/ui-apply-button-width-safe.png`
- Sample-detector layout proof for text fit and arrow placement: `.scratch/planning/2026-07-22-winui-recovery/evidence/ui-sample-header-safe.png`

The current desktop session changed after those captures. Win32 now reports one
display and the cached COM monitor paths fail `GetMonitorRECT`, so a fresh
three-monitor native capture is blocked by the external display state. The
application still uses `WindowsMonitorDetector`; the sample detector was used
only for the isolated layout proof and is not part of the shipped path.

The selected visual and the final native capture were joined into one 3072 × 1024 comparison and inspected together. The final screen now follows the same measured structure: 280 px monitor rail, 2–1–3 physical monitor order, large centered stage, editor beginning near y=600, three editor columns, 2 × 6 color palette, 3 × 3 anchor grid, 80 px detection action, and a thin status footer.

The generated reference uses sample wallpaper photos. The native capture keeps the three real solid-color assignments loaded from Windows, so their previews remain black. Waller does not show a drag hint or background-color toggle because those actions do not exist in the current model; the implementation does not present controls that cannot work.

Evidence:

- Native capture: `.scratch/planning/2026-07-22-winui-recovery/evidence/ui-composer-accepted.png`
- Joined comparison: `.scratch/planning/2026-07-22-winui-recovery/evidence/reference-vs-native-accepted.png`
- UI Automation measured the outer window at 1536 × 1024, position x=952/y=183, centered in the 3440 × 1390 active work area.
- The placeholder WinUI artwork was replaced with the generated Waller mountain-and-lake source at `Waller.Native.App/Assets/WallerAppIconSource.png` and derived Windows icon sizes.

| Check | Result |
| --- | --- |
| Build, x64 Debug | Passed: 0 warnings, 0 errors |
| Core and policy tests | Passed: 467/467 |
| Default 1536 × 1024 size and centered placement | Passed through UI Automation geometry |
| Rail, stage, and editor match against selected visual | Passed through joined-image review |
| More menu, settings, save-as, and preset management smoke | Passed: Shell 5/5, Settings 5/5, Save As 3/3, Manage Presets 6/6 |
| Accessibility and theme surface | Passed: 15 XAML files plus WinUI code guards |

Final result: passed for the native, functional adaptation of the selected visual.
