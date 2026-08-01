---
id: ARC-09
status: completed
type: AFK
blocked_by: [ARC-02, ARC-03]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-09: Consolidar Apply

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Concentrar ejecución, cancelación y resultado técnico en ApplyWorkflow. Entregar una ApplyViewModel que traduzca progreso y outcome para la UI.

El workflow no contendrá texto localizado. ShellWorkspace impedirá un segundo Apply concurrente.

## Acceptance criteria / Criterios de aceptación

- [x] Run all y Run monitor usan una interfaz común.
- [x] Solo existe una ejecución activa por proceso.
- [x] Cancel solicita cancelación una vez y libera recursos una vez.
- [x] El outcome conserva la Active Session actualizada cuando existe.
- [x] La cancelación conserva resultados parciales.
- [x] Una falla parcial no revierte monitores exitosos.
- [x] ApplyViewModel posee el texto y el progreso localizado.
- [x] Los controles de Apply no dependen de todo el ViewModel principal.

## Blocked by

- ARC-02: Introducir Workflows y ShellWorkspace.
- ARC-03: Centralizar la composición de la App.

## User stories covered

22, 23, 24, 25, 26, 32.

## Verification

- Ejecutar éxito, no-op, falla parcial, cancelación y excepción inesperada.
- Usar los adapters falsos existentes y observar solo outcomes públicos.

## Evidence / Evidencia

- `ApplyWorkflowTests`: 7/7 all/monitor, no-op, partial failure, cancellation, concurrency and unexpected-failure scenarios.
- Apply/composition/shell/WinUI/XAML guards: pass.
- App x64 Release build: pass, 0 warnings, 0 errors.
- Apply surface and real Windows adapter smoke are part of ARC-10 final integration.
