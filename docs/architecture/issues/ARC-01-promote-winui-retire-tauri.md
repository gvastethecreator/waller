---
id: ARC-01
status: completed
type: AFK
blocked_by: []
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-01: Promover WinUI y retirar Tauri

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Convertir la raíz en la entrada única al producto WinUI. Retirar Tauri y el prototipo anterior sin tocar datos del usuario.

El ticket también debe alinear CI, release, seguridad, contribución, vocabulario y comandos. Git conservará la historia eliminada.

## Acceptance criteria / Criterios de aceptación

- [x] La raíz describe y ejecuta solo el producto WinUI.
- [x] El código Tauri y el prototipo anterior ya no forman parte del árbol rastreado.
- [x] La documentación activa usa `Preset` y `Active Session`.
- [x] CI y release producen artefactos WinUI.
- [x] La versión requerida de .NET queda definida para colaboradores y CI.
- [x] Las dos guardas rojas del baseline vuelven a pasar.
- [x] La limpieza no lee, migra ni borra datos del usuario.
- [x] Las referencias Tauri restantes están marcadas como historia o ADR.

## Evidence / Evidencia

- Root gate: `Invoke-Native.ps1 -Task Verify -SkipSmoke -DisableNuGetAudit` passed with 470 tests.
- Release gate: `Invoke-Native.ps1 -Task Release -Platform x64 -DisableNuGetAudit` passed.
- Workflow YAML, Markdown links, root scripts, and deletion ledger passed structural checks.
- `src/`, `src-tauri/`, and `prototypes/winui-native/` are absent on disk; their tracked files remain as unstaged deletions until the user chooses to commit.

## Blocked by

- None - can start immediately.

## User stories covered

1, 2, 28, 29, 36, 37, 38, 39.

## Verification

- Ejecutar el baseline nativo antes y después de la limpieza.
- Comprobar referencias, enlaces, comandos raíz y estructura de CI/release.
- Comprobar que Git no rastrea las tres líneas obsoletas.
