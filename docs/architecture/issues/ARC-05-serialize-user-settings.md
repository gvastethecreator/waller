---
id: ARC-05
status: completed
type: AFK
blocked_by: [ARC-03, ARC-04]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-05: Serializar UserSettings

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Crear un único workflow para leer y actualizar UserSettings. Serializar todas las mutaciones y preservar campos ajenos a cada acción.

Settings, selección de Preset y posición de ventana deben usar el mismo escritor.

## Acceptance criteria / Criterios de aceptación

- [x] Existe un solo escritor de UserSettings por proceso.
- [x] Las actualizaciones concurrentes se ejecutan en orden.
- [x] Guardar preferencias preserva la posición de ventana.
- [x] Guardar posición preserva tema, idioma y Preset seleccionado.
- [x] Persistir el Preset seleccionado preserva los demás campos.
- [x] Una cancelación no publica un archivo parcial.
- [x] Un error recuperable produce un resultado tipado.
- [x] Las pruebas usan un directorio temporal y el store real.

## Blocked by

- ARC-03: Centralizar la composición de la App.
- ARC-04: Hacer ejecutable la política de datos locales.

## User stories covered

6, 7, 8, 33.

## Verification

- Ejecutar carreras deterministas de actualizaciones.
- Ejecutar pruebas de preservación y cancelación.

## Evidence / Evidencia

- `UserSettingsWorkflowTests`: 6/6.
- Writer/composition/Core/WinUI guards: pass.
- Packaged x64 Debug build: pass, 0 warnings, 0 errors.
- Settings round-trip surface smoke: pass.
