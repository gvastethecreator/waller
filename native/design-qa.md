# Design QA: monitor composer

## Target

Selected visual: option 3 from the product-design review. The target establishes a light native desktop workspace with a persistent monitor rail, a central monitor stage, compact primary actions, and a direct editor below the stage.

## Native adaptation

- Waller uses the monitor assignment that is actually loaded. The rail and stage render a real image only when that assignment contains an existing image file; solid-color assignments keep their true color preview.
- The window starts at 1520 × 960 for new settings. Existing saved window placement remains untouched.
- At 1120 × 760, the rail retains all three loaded monitors, the stage keeps all tiles visible, and the editor remains usable without horizontal clipping.
- The editor reacts to monitor selection from either the rail or the stage. The compact `Más` menu keeps save, preset, settings, and refresh actions reachable.

## Comparison and checks

Reference and native capture were inspected together on 2026-07-22. The resulting screen preserves the selected layout hierarchy: preset and primary action above, monitor rail on the left, monitor map in the center, and editor below. Differences in wallpaper imagery reflect the real loaded sources rather than generated sample images.

| Check | Result |
| --- | --- |
| Build, x64 Debug | Passed: 0 warnings, 0 errors |
| Screen layout at 1120 × 760 | Passed |
| Rail, stage, and editor selection path | Passed |
| More menu, settings, save-as, and preset management smoke | Passed |
| Focusable monitor descriptions | Passed through UI Automation |

Final result: passed for the native adaptation of the selected visual.
