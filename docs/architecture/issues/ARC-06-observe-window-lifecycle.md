---
id: ARC-06
status: completed
type: AFK
blocked_by: [ARC-03, ARC-05]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-06: Observar el ciclo de vida de la ventana

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Separar la carga y el guardado de Window Placement del evento WinUI. Observar ambas tareas y presentar la ventana después de restaurar.

El cierre debe esperar el guardado o aceptar una falla recuperable antes de destruir la ventana.

## Acceptance criteria / Criterios de aceptación

- [x] La restauración termina antes de la activación visible final.
- [x] Una posición inválida usa la política de fallback existente.
- [x] La tarea de restauración no se descarta.
- [x] El primer cierre se cancela mientras el workflow guarda.
- [x] La ventana se destruye después del guardado o de una falla recuperable.
- [x] El guardado no inicia un segundo ciclo de cierre.
- [x] La última geometría válida llega a UserSettingsWorkflow.
- [x] Un smoke comprueba restauración, cierre y proceso terminado.

## Blocked by

- ARC-03: Centralizar la composición de la App.
- ARC-05: Serializar UserSettings.

## User stories covered

4, 5, 8.

## Verification

- Ejecutar pruebas del workflow con geometrías válidas e inválidas.
- Ejecutar el smoke de cierre y comprobar la Settings persistida.

## Evidence / Evidencia

- `WindowPlacementWorkflowTests`: 5/5.
- Window lifecycle/composition/writer guards: pass.
- Packaged x64 Debug build: pass, 0 warnings, 0 errors.
- Restore/close/process lifecycle smoke: pass; final geometry `1180x760@80,70`.
