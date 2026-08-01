---
id: ARC-08
status: completed
type: AFK
blocked_by: [ARC-02, ARC-07]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-08: Consolidar el editor de monitor

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Concentrar selección, draft y edición de assignments en MonitorEditorWorkflow. Entregar una MonitorEditorViewModel acotada a la superficie de edición.

El workflow actualizará la Active Session una vez por outcome. Nunca guardará Presets ni aplicará wallpapers.

## Acceptance criteria / Criterios de aceptación

- [x] Seleccionar un monitor crea un draft válido desde su assignment.
- [x] Cambiar source produce un outcome tipado.
- [x] Cambiar placement y offsets conserva la normalización de Core.
- [x] Una imagen faltante produce una falla visible sin tocar Windows.
- [x] Forget y Reassign operan sobre assignments desconectados.
- [x] Cada outcome reemplaza la Active Session como una operación.
- [x] La superficie de edición depende de MonitorEditorViewModel.
- [x] Se conservan accesibilidad, localización y bindings visibles.

## Blocked by

- ARC-02: Introducir Workflows y ShellWorkspace.
- ARC-07: Consolidar el flujo de Presets.

## User stories covered

16, 17, 18, 19, 20, 21, 27, 32.

## Verification

- Ejecutar pruebas de source, placement, offsets y path faltante.
- Ejecutar pruebas de Forget y Reassign con topologías cambiantes.

## Evidence / Evidencia

- `MonitorEditorWorkflowTests`: 7/7 source, placement, missing-image, Forget y Reassign scenarios.
- Monitor editor, composition, WinUI, accessibility, localization, modal and shell guards: pass.
- App x64 Release build: pass, 0 warnings, 0 errors.
- Packaged monitor editor surface smoke: pass with Source, Fit, Anchor and Reset controls visible.
