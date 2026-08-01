---
id: ARC-02
status: completed
type: AFK
blocked_by: [ARC-01]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-02: Introducir Workflows y ShellWorkspace

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Añadir un módulo de workflows sin XAML. Crear un ShellWorkspace que posea la Active Session, la pila modal y el lease de Apply.

La App debe consumir esa interfaz. Tests debe ejecutar sus transiciones sin iniciar una ventana.

## Acceptance criteria / Criterios de aceptación

- [x] Workflows depende solo de Core.
- [x] App y Tests consumen Workflows mediante referencias compiladas.
- [x] ShellWorkspace reemplaza la Active Session mediante una operación explícita.
- [x] La pila modal no permite combinaciones inválidas.
- [x] Cerrar el modal superior conserva el modal padre cuando corresponde.
- [x] Solo existe un lease activo de Apply.
- [x] Cada transición tiene una prueba pública.
- [x] Las guardas textuales equivalentes salen después de esas pruebas.

## Evidence / Evidencia

- `ShellWorkspaceTests`: 8/8 public transition tests passed.
- Native solution compiled with Core, Workflows, App, and Tests: 0 warnings and 0 errors.
- Full test project passed 478/478; WinUI and modal keyboard guards passed.
- Structural check confirmed `Workflows -> Core`, App/Tests references, and removal of retired textual state guards.

## Blocked by

- ARC-01: Promover WinUI y retirar Tauri.

## User stories covered

22, 30, 31, 32, 40.

## Verification

- Ejecutar las pruebas de ShellWorkspace.
- Compilar Core, Workflows, App y Tests con la dirección acordada.
