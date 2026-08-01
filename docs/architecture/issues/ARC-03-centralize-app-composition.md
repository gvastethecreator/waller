---
id: ARC-03
status: completed
type: AFK
blocked_by: [ARC-02]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-03: Centralizar la composición de la App

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Crear una sola gráfica de dependencias por proceso. La ventana y la página deben compartir stores, workflows y ViewModels.

Eliminar los globales de ventana, HWND y dispatcher. Entregar el HWND concreto al picker que lo necesita.

## Acceptance criteria / Criterios de aceptación

- [x] La App ejecuta una sola composición por proceso.
- [x] La ventana y la página reciben dependencias mediante construcción explícita.
- [x] Settings y Presets usan las mismas instancias compartidas.
- [x] No quedan globales estáticos de ventana, HWND o dispatcher.
- [x] El picker recibe un owner válido sin consultar estado global.
- [x] No se añade un contenedor DI.
- [x] No se añade un port con un solo adapter.
- [x] La aplicación inicia y el picker conserva su contrato.

## Evidence / Evidencia

- App composition contract passed with one default store graph and concrete picker owner.
- App project compiled with 0 warnings and 0 errors.
- Packaged launch smoke passed: process `Waller.Native.App`, title `Waller`, responding.
- Static global scan found no App Window, HWND, or dispatcher globals.
- The OS file dialog was not selected during automation; the picker owner/initialization path compiled and passed its focused contract guard.

## Blocked by

- ARC-02: Introducir Workflows y ShellWorkspace.

## User stories covered

3, 30, 31, 32.

## Verification

- Ejecutar las pruebas de composición.
- Ejecutar un smoke de inicio y un smoke focal del picker.
