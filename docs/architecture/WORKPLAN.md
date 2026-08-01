---
id: WORKPLAN-WALLER-WINUI-DEFINITIVE
status: completed
language: es
spec: SPEC-WALLER-WINUI-DEFINITIVE
ticket_count: 10
updated: 2026-08-01
---

# Workplan: arquitectura definitiva de Waller WinUI

Este tracker convierte la especificación aprobada en 10 mejoras ejecutables. La fuente canónica es [la especificación](./winui-definitive-architecture-spec.md).

Trabajar el frontier de a un ticket. Un ticket entra al frontier cuando todos sus blockers están completos.

## Estado

| ID | Ticket | Tipo | Estado | Blocked by | Historias |
|---|---|---|---|---|---|
| ARC-01 | [Promover WinUI y retirar Tauri](./issues/ARC-01-promote-winui-retire-tauri.md) | AFK | completed | Ninguno | 1, 2, 28, 29, 36-39 |
| ARC-02 | [Introducir Workflows y ShellWorkspace](./issues/ARC-02-introduce-workflows-shell-workspace.md) | AFK | completed | ARC-01 | 22, 30-32, 40 |
| ARC-03 | [Centralizar la composición](./issues/ARC-03-centralize-app-composition.md) | AFK | completed | ARC-02 | 3, 30-32 |
| ARC-04 | [Hacer ejecutable la política de datos locales](./issues/ARC-04-executable-local-data-layout.md) | AFK | completed | ARC-02 | 2, 33 |
| ARC-05 | [Serializar UserSettings](./issues/ARC-05-serialize-user-settings.md) | AFK | completed | ARC-03, ARC-04 | 6-8, 33 |
| ARC-06 | [Observar el ciclo de vida de la ventana](./issues/ARC-06-observe-window-lifecycle.md) | AFK | completed | ARC-03, ARC-05 | 4, 5, 8 |
| ARC-07 | [Consolidar el flujo de Presets](./issues/ARC-07-deepen-preset-workflow.md) | AFK | completed | ARC-02, ARC-05 | 9-15, 29, 32 |
| ARC-08 | [Consolidar el editor de monitor](./issues/ARC-08-deepen-monitor-editor.md) | AFK | completed | ARC-02, ARC-07 | 16-21, 27, 32 |
| ARC-09 | [Consolidar Apply](./issues/ARC-09-deepen-apply-workflow.md) | AFK | completed | ARC-02, ARC-03 | 22-26, 32 |
| ARC-10 | [Reorganizar las pruebas y cerrar el lote](./issues/ARC-10-reorganize-verification.md) | AFK | completed | ARC-04, ARC-05, ARC-06, ARC-07, ARC-08, ARC-09 | 27, 34-38, 40 |

## Frontier

- ARC-01 a ARC-10 están completos.
- No quedan tickets en el frontier de este lote.

## Preflight de ARC-01

- Corregir el lint de localización para los encabezados X/Y.
- Corregir el uso de la variable automática `$Error` en la guarda del shell.
- Proveer .NET SDK 10 mediante un toolchain local si el host sigue sin él.
- Registrar un baseline nativo antes de borrar la línea Tauri.

## Reglas de ejecución

- Mantener exactamente 10 tickets superiores.
- Usar subtasks para cada secuencia expand-contract.
- Mantener una forma verde antes de borrar la anterior.
- Actualizar el estado y la evidencia antes de tomar otro ticket.
- Probar el comportamiento público, no snippets internos.
- Preservar `.scratch/`, `native/screenshot.png` y todo cambio ajeno.
- No borrar datos de usuario durante la limpieza del repositorio.
- No crear commits, push ni PR sin autorización explícita.

## Prueba final

ARC-10 ejecutará una sola integración completa. Esta prueba incluirá build Release, surface smoke, Settings roundtrip y Apply smoke restaurable.

Los siguientes gates quedan fuera de este host: Store, firma de producción, clean-machine y runtime ARM64.

## Evidencia

| Ticket | Estado | Prueba focal | Resultado |
|---|---|---|---|
| ARC-01 | completed | Gate raíz + Release x64 + estructura | pass: 470/470 |
| ARC-02 | completed | 8 transiciones públicas + solución | pass: 478/478 |
| ARC-03 | completed | Contrato + build + packaged launch | pass |
| ARC-04 | completed | 9 escenarios + guarda + Settings roundtrip | pass |
| ARC-05 | completed | 6 escenarios + guardas + Settings roundtrip | pass |
| ARC-06 | completed | 5 escenarios + guarda + lifecycle smoke | pass |
| ARC-07 | completed | 5 escenarios + guardas + surface smoke | pass |
| ARC-08 | completed | 7 scenarios + guards + monitor editor smoke | pass |
| ARC-09 | completed | 7 scenarios + guards + Release build | pass |
| ARC-10 | completed | 517 tests + packaged smokes + Release x64 + reports | pass |

## Cierre

La implementación y el riesgo residual están registrados en el
[informe final](./winui-definitive-architecture-report.md) y su
[versión HTML](./winui-definitive-architecture-report.html).
