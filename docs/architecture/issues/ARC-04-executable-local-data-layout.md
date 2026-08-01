---
id: ARC-04
status: completed
type: AFK
blocked_by: [ARC-02]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-04: Hacer ejecutable la política de datos locales

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Convertir el cálculo de raíces locales en una política pura con entradas explícitas. Mantener la lectura del entorno Windows en la App.

Presets y Settings deben conservar su raíz privada. Rendered Wallpaper debe conservar una raíz que Windows pueda leer.

## Acceptance criteria / Criterios de aceptación

- [x] La política recibe rutas explícitas y produce un layout tipado.
- [x] El layout rechaza entradas vacías o relativas.
- [x] El escenario empaquetado produce raíces deterministas.
- [x] El escenario no empaquetado produce raíces deterministas.
- [x] Los stores se construyen desde un solo layout.
- [x] No cambia el esquema ni la ubicación efectiva aprobada.
- [x] La guarda textual deja de inspeccionar detalles cubiertos por pruebas.
- [x] Los smokes conservan backup y restauración de datos locales.

## Blocked by

- ARC-02: Introducir Workflows y ShellWorkspace.

## User stories covered

2, 33.

## Verification

- Ejecutar escenarios directos de layout empaquetado y normal.
- Ejecutar las guardas de package y el Settings roundtrip cuando el ticket quede integrado.

## Evidence / Evidencia

- `LocalDataLayoutTests`: 9/9.
- `TestLocalDataPolicy.ps1`: pass.
- App build x64 Debug: pass, 0 warnings, 0 errors.
- `SmokeSurface.ps1 -SettingsRoundTrip`: pass; backup and restoration retained.
