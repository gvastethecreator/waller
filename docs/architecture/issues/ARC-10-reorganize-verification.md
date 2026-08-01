---
id: ARC-10
status: completed
type: AFK
blocked_by: [ARC-04, ARC-05, ARC-06, ARC-07, ARC-08, ARC-09]
spec: SPEC-WALLER-WINUI-DEFINITIVE
---

# ARC-10: Reorganizar las pruebas y cerrar el lote

## Parent

SPEC-WALLER-WINUI-DEFINITIVE

## What to build / Qué construir

Organizar las pruebas por módulo público y retirar guardas que solo fijan implementación. Conservar todos los lints y contratos sin seam compilado equivalente.

Ejecutar la integración completa y publicar los informes finales del lote.

## Acceptance criteria / Criterios de aceptación

- [x] Las pruebas Core están separadas por dominio y ownership.
- [x] Workflows tiene pruebas directas para cada flujo nuevo.
- [x] Ninguna aserción existente se debilita para obtener verde.
- [x] El detector de monitores de muestra vive solo como fixture de prueba.
- [x] Las guardas de snippets salen solo cuando una prueba pública las reemplaza.
- [x] Los lints XAML, package, firma y seguridad aplicables siguen activos.
- [x] La integración completa pasa una vez sobre el estado final.
- [x] Los informes Markdown y HTML describen valor obtenido y riesgo residual.

## Blocked by

- ARC-04: Hacer ejecutable la política de datos locales.
- ARC-05: Serializar UserSettings.
- ARC-06: Observar el ciclo de vida de la ventana.
- ARC-07: Consolidar el flujo de Presets.
- ARC-08: Consolidar el editor de monitor.
- ARC-09: Consolidar Apply.

## User stories covered

27, 34, 35, 36, 37, 38, 40.

## Verification

- Ejecutar la suite completa, build Release y smokes restaurables.
- Registrar los gates no ejecutados sin convertirlos en pruebas aprobadas.

## Evidence / Evidencia

- Core se divide en 9 módulos de dominio y un fixture compartido; el monolito anterior salió.
- Los 7 workflows públicos tienen suites directas.
- `SampleMonitorDetector` vive en `Waller.Native.Tests/Fixtures`.
- Gate final: 517/517 tests, todos los guardas, launch/lifecycle/surface/Settings/Apply empaquetados y Release x64.
- Apply cambió y restauró 3 fondos de monitor; 3/3 finalizaron correctamente.
- Informes finalizados en Markdown y HTML.
