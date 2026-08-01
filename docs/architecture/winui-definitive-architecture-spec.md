---
id: SPEC-WALLER-WINUI-DEFINITIVE
status: completed
language: es
approved: 2026-08-01
ticket_count: 10
---

# Especificación de arquitectura definitiva para Waller WinUI

## Problem Statement / Declaración del problema

Waller mantiene tres líneas de producto dentro del mismo repositorio. La raíz todavía presenta Tauri como producto, pero WinUI ya es la versión definitiva.

La duplicación afecta comandos, CI, release, documentación y vocabulario. También obliga a mantener dos implementaciones que ya no comparten un destino de producto.

La aplicación WinUI concentra sus flujos en un solo ViewModel. Sus reglas de aplicación tienen poca prueba directa y muchas guardas inspeccionan texto interno.

La composición crea dependencias por separado para la ventana y la página. Settings, selección de Preset y posición de ventana pueden escribir el mismo JSON.

## Solution / Solución

La raíz del repositorio representará solo el producto WinUI. El código Tauri y el prototipo anterior saldrán del árbol activo sin borrar datos del usuario.

Un módulo Workflows contendrá los casos de uso sin depender de XAML. Core conservará el dominio y los adapters de Windows.

La App quedará limitada a composición, controles WinUI y proyección localizada. Cada superficie dependerá de un ViewModel acotado.

Un ShellWorkspace será dueño de la Active Session, los modales y la exclusión de Apply. Un workflow serializará todas las escrituras de UserSettings.

Las pruebas usarán las interfaces públicas de Core y Workflows. Las guardas textuales quedarán solo donde no exista una interfaz ejecutable equivalente.

## User Stories / Historias de usuario

1. Como usuario de Waller, quiero una sola aplicación oficial, para saber qué versión instalar y usar.
2. Como usuario de Waller, quiero que la limpieza del repositorio conserve mis datos locales, para no perder Presets ni preferencias.
3. Como usuario de Waller, quiero que la aplicación inicie con mi configuración actual de Windows, para editarla sin cambios automáticos.
4. Como usuario de Waller, quiero que mi ventana aparezca en una posición válida, para no buscar una ventana fuera del escritorio.
5. Como usuario de Waller, quiero que la posición de la ventana se guarde al cerrar, para recuperar mi espacio de trabajo.
6. Como usuario de Waller, quiero que tema e idioma sobrevivan al cierre, para mantener mis preferencias.
7. Como usuario de Waller, quiero que guardar Settings preserve la posición de ventana, para no perder otra preferencia.
8. Como usuario de Waller, quiero que cerrar la ventana preserve Settings recientes, para evitar una escritura perdida.
9. Como usuario de Waller, quiero seleccionar un Preset sin cambiar Windows, para revisar la composición antes de aplicarla.
10. Como usuario de Waller, quiero guardar la Active Session como Preset, para reutilizar mi composición.
11. Como usuario de Waller, quiero renombrar un Preset, para mantener una lista comprensible.
12. Como usuario de Waller, quiero duplicar un Preset, para crear una variante sin reemplazar el original.
13. Como usuario de Waller, quiero borrar un Preset con confirmación, para evitar una eliminación accidental.
14. Como usuario de Waller, quiero conservar la Active Session después de borrar su Preset, para no perder ediciones actuales.
15. Como usuario de Waller, quiero una respuesta clara cuando falta un Preset, para refrescar la lista sin reiniciar.
16. Como usuario de Waller, quiero editar el Wallpaper Source de un monitor, para preparar su fondo sin tocar Windows.
17. Como usuario de Waller, quiero editar Placement y offsets, para controlar el encuadre de cada monitor.
18. Como usuario de Waller, quiero ver un error útil para una imagen faltante, para corregir el path antes de Apply.
19. Como usuario de Waller, quiero conservar asignaciones de monitores desconectados, para reutilizarlas cuando vuelva el hardware.
20. Como usuario de Waller, quiero reasignar un monitor desconectado, para adaptar un Preset a la topología actual.
21. Como usuario de Waller, quiero olvidar una asignación desconectada, para quitar contenido que ya no necesito.
22. Como usuario de Waller, quiero ejecutar Apply una sola vez, para evitar operaciones concurrentes sobre el escritorio.
23. Como usuario de Waller, quiero cancelar Apply, para detener una operación que ya no deseo.
24. Como usuario de Waller, quiero conservar resultados parciales tras cancelar, para conocer qué monitores cambiaron.
25. Como usuario de Waller, quiero que una falla parcial no revierta monitores correctos, para conservar trabajo válido.
26. Como usuario de Waller, quiero texto localizado separado del resultado técnico, para recibir mensajes correctos en mi idioma.
27. Como usuario con lector de pantalla, quiero que la refactorización conserve nombres accesibles, para operar la aplicación sin pérdida funcional.
28. Como colaborador, quiero un comando raíz para WinUI, para ejecutar la prueba correcta sin conocer la historia de Tauri.
29. Como colaborador, quiero documentación con `Preset` y `Active Session`, para usar el vocabulario definitivo.
30. Como colaborador, quiero una sola gráfica de composición, para conocer la vida de cada dependencia.
31. Como colaborador, quiero workflows sin XAML, para probar reglas sin iniciar WinUI.
32. Como colaborador, quiero ViewModels acotados por superficie, para cambiar una función sin revisar todo MainPageViewModel.
33. Como colaborador, quiero una política ejecutable de rutas locales, para probar entornos empaquetados y no empaquetados.
34. Como colaborador, quiero pruebas agrupadas por módulo, para localizar fallas y ownership con rapidez.
35. Como colaborador, quiero guardas que comprueben contratos, para permitir refactors internos correctos.
36. Como responsable de CI, quiero un toolchain .NET definido, para evitar que un SDK incorrecto bloquee la compilación.
37. Como responsable de release, quiero artefactos WinUI, para no publicar instaladores Tauri obsoletos.
38. Como responsable de seguridad, quiero conservar la postura Windows-only, para evitar abstracciones sin uso y permisos nuevos.
39. Como mantenedor, quiero conservar la historia en Git, para consultar Tauri sin mantenerlo como producto activo.
40. Como agente de implementación, quiero tickets con dependencias explícitas, para trabajar solo el frontier válido.

## Implementation Decisions / Decisiones de implementación

- WinUI será el único producto activo. Tauri y el prototipo anterior permanecerán disponibles mediante la historia de Git.
- El vocabulario canónico usará Monitor, Active Session, Preset, Wallpaper Source, Placement y Rendered Wallpaper.
- Core conservará modelos, persistencia, render, detección y adapters de Windows.
- Workflows dependerá solo de Core. Workflows no usará XAML, Brushes, Visibility ni tipos de ventana.
- App dependerá de Core y Workflows. App conservará controles, binding, localización y adapters de ventana.
- Tests dependerá de Core y Workflows. Las pruebas no accederán a miembros privados para controlar una operación.
- ShellWorkspace será el dueño de la Active Session y de las transiciones del shell.
- ShellWorkspace representará los modales como una pila válida. No conservará cuatro booleanos independientes.
- ShellWorkspace entregará un lease exclusivo para Apply. Un segundo Apply fallará antes de tocar Windows.
- La composición creará una sola gráfica por proceso. La ventana y la página compartirán las mismas instancias.
- App no expondrá la ventana, el HWND ni el dispatcher mediante miembros estáticos globales.
- El picker recibirá el HWND concreto. No se creará un port mientras exista un solo adapter necesario.
- LocalDataLayout calculará las raíces desde entradas explícitas. La lectura del entorno Windows quedará en App.
- Los datos JSON nativos conservarán su esquema. El lote no añade compatibilidad con Profiles Tauri.
- UserSettingsWorkflow será el único escritor de UserSettings. Cada actualización preservará campos ajenos.
- La cola de UserSettings serializará escrituras. La cancelación no publicará un archivo parcial.
- WindowPlacementWorkflow separará la política de posición del evento de ventana.
- La restauración terminará antes de mostrar la ventana en su posición final.
- El primer cierre se cancelará mientras se guarda la geometría. La ventana se destruirá después de terminar.
- PresetWorkflow poseerá catálogo, selección, guardado y mutaciones de Presets.
- PresetsViewModel expondrá solo el estado y los comandos usados por las superficies de Presets.
- MonitorEditorWorkflow poseerá el draft y producirá outcomes tipados.
- MonitorEditorWorkflow no guardará Presets ni aplicará wallpapers.
- MonitorEditorViewModel será el único dueño del estado y los comandos de la superficie de edición.
- Un outcome exitoso de edición reemplazará la Active Session una sola vez mediante ShellWorkspace.
- ApplyWorkflow poseerá servicio, cancelación y resultado técnico. ApplyWorkflow no producirá texto localizado.
- ApplyViewModel traducirá progreso y resultados a la superficie localizada.
- ApplyWorkflow adquirirá y liberará un único lease de ShellWorkspace por ejecución.
- La cancelación devolverá el resultado técnico parcial cuando Core lo produzca.
- SettingsWorkflow poseerá preferencias y limpieza del rendered cache.
- SettingsViewModel expondrá solo la superficie Settings.
- La migración usará expand-contract. Cada forma nueva existirá antes de migrar y borrar la forma anterior.
- La raíz ofrecerá comandos WinUI para desarrollo, prueba, release y CI.
- Las referencias históricas a Tauri quedarán marcadas como historia. No aparecerán como instrucciones activas.

## Testing Decisions / Decisiones de prueba

- Una buena prueba observa el resultado público de un módulo. No comprueba nombres privados, orden interno ni ubicación de archivos.
- Las pruebas de ShellWorkspace cubrirán sesiones, modales, permisos y exclusión de Apply.
- Las pruebas de LocalDataLayout cubrirán rutas empaquetadas, rutas normales y fallbacks válidos.
- Las pruebas de UserSettingsWorkflow cubrirán escrituras concurrentes y preservación de campos.
- Las pruebas de WindowPlacementWorkflow cubrirán carga, guardado, errores recuperables y geometría válida.
- Las pruebas de PresetWorkflow usarán un directorio temporal y el PresetStore real.
- Las pruebas de MonitorEditorWorkflow cubrirán sources, placement, offsets y monitores desconectados.
- Las pruebas de ApplyWorkflow usarán los adapters falsos existentes para éxito, falla parcial y cancelación.
- Las pruebas Core existentes se dividirán por dominio sin reducir aserciones ni casos.
- Los lints XAML seguirán comprobando accesibilidad, localización y contratos que el compilador no observa.
- Las guardas de package, firma y scripts seguirán activas cuando protejan un riesgo operativo real.
- Las guardas que exigen snippets internos saldrán después de existir una prueba pública equivalente.
- Cada ticket ejecutará la prueba focal más pequeña que pueda invalidar su cambio.
- El lote ejecutará una sola integración completa después de migrar todos los callers.
- La integración incluirá build Release, surface smoke, Settings roundtrip y Apply smoke con restauración.
- La prueba no borrará Presets ni Settings. Los smokes conservarán backup y restauración en `finally`.
- La firma de producción, Store, clean-machine y ARM64 runtime quedarán declarados como no ejecutados.
- La suite actual de Core y los scripts nativos son el prior art. Workflows añadirá la nueva prueba directa.

## Out of Scope / Fuera de alcance

- Un rediseño visual de la interfaz WinUI.
- Nuevas funciones de wallpaper, editor, Presets o Settings.
- Importación de Profiles Tauri.
- Migración o eliminación de datos Tauri en perfiles de usuario.
- Cambios del esquema JSON nativo.
- Publicación en Microsoft Store.
- Firma con certificado de producción.
- Compatibilidad multiplataforma.
- Runtime ARM64 y clean-machine en este equipo.
- Poda completa del historial acumulado en los documentos nativos.

## Further Notes / Notas adicionales

- El contrato fue aprobado el 2026-08-01.
- La línea base contiene dos fallas previas de guardas. El lote las corregirá antes del primer ticket estructural.
- El shell actual solo encuentra .NET SDK 9. El trabajo usará una copia local oficial de .NET SDK 10.
- La cola contiene exactamente 10 tickets. Los subtasks de expand-contract no aumentan esa cantidad.
- `docs/architecture/WORKPLAN.md` es el tracker local canónico para este lote.
