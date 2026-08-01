---
id: ARC-07
status: completed
type: AFK
blocked_by: [ARC-02, ARC-05]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-07: Consolidar el flujo de Presets

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Concentrar catálogo, selección, guardado y mutaciones en PresetWorkflow. Entregar una PresetsViewModel acotada a las superficies de Presets.

La selección seguirá sin tocar Windows. Borrar el Preset activo conservará la Active Session.

## Acceptance criteria / Criterios de aceptación

- [x] PresetWorkflow posee lista, selección, guardado, rename, duplicate y delete.
- [x] Las operaciones devuelven outcomes tipados para éxito, faltante y falla de escritura.
- [x] Seleccionar Current setup conserva la sesión actual de Windows.
- [x] Seleccionar un Preset no ejecuta Apply.
- [x] Borrar el Preset activo conserva la Active Session.
- [x] PresetsViewModel expone solo estado y comandos de sus controles.
- [x] Los controles de Presets no dependen de todo el ViewModel principal.
- [x] Los helpers pasantes salen después de migrar todos los callers.

## Blocked by

- ARC-02: Introducir Workflows y ShellWorkspace.
- ARC-05: Serializar UserSettings.

## User stories covered

9, 10, 11, 12, 13, 14, 15, 29, 32.

## Verification

- Ejecutar el flujo completo con un store temporal real.
- Cubrir Preset faltante, escritura fallida y borrado del Preset activo.

## Evidence / Evidencia

- `PresetWorkflowTests`: 5/5 full-flow scenarios against a real temporary store.
- Preset/composition/modal/shell/WinUI guards: pass.
- Packaged x64 Debug build: pass, 0 warnings, 0 errors.
- Shell, Save As, and Manage Presets surface smoke: pass.
