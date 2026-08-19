# Waller Privacy Policy

**Effective date:** August 17, 2026  
**Publisher:** To be completed with the verified Microsoft Store publisher name before submission

Waller is a local-first Windows wallpaper manager for multi-monitor setups. It lets users choose local images or colors, arrange wallpaper placement per monitor, save presets, render wallpaper files, and apply them through Windows desktop APIs.

This policy explains what Waller accesses, what it stores, and what it does not transmit.

## 1. Data Waller may access

Waller may access the following information on the user's Windows device:

- connected-monitor identifiers, bounds, orientation, and topology;
- the current wallpaper path reported by Windows for each monitor;
- local image files explicitly selected by the user;
- solid-color and placement choices;
- preset names and preset configuration;
- application settings such as language and appearance;
- temporary/rendered wallpaper images created to apply the selected composition;
- operational errors required to explain why a local file or Windows operation failed.

Waller accesses this information only to provide wallpaper preview, preset, rendering, and apply functionality.

## 2. Local storage

The packaged application stores presets and settings under its Windows package-local application-data area. The effective packaged path includes:

```text
%LOCALAPPDATA%\Packages\<package-family-name>\LocalCache\Local\Waller
```

Rendered wallpaper PNG files are stored in a shell-readable user-profile location:

```text
%USERPROFILE%\.waller\rendered
```

The rendered location is required because Windows desktop wallpaper APIs must be able to read the resulting files outside package-virtualized storage.

Local records may contain image paths, monitor assignments, placement choices, preset names, and application preferences. Users should avoid using sensitive information in preset names or file paths if those names may later appear in screenshots or support material.

## 3. Network activity and transmission

Waller does not require a Waller account.

Waller is not designed to upload wallpaper images, rendered images, file paths, monitor information, presets, or settings to GVASTETHECREATOR or another service.

The following activities may involve software outside Waller:

- Microsoft Store delivery, licensing, crash reporting, and update infrastructure operated by Microsoft;
- a website, repository, documentation, or support link that the user explicitly chooses to open in the default browser;
- network-backed image paths selected and managed by the user through Windows or another application.

Waller itself does not provide a cloud wallpaper service in this release.

## 4. Personal information

Waller does not intentionally collect names, email addresses, account credentials, advertising identifiers, contact lists, browsing history, prompts, conversations, or document contents.

Local file paths and preset names can indirectly contain personal information chosen by the user. Waller keeps these values on the device and uses them only for the requested local workflow.

## 5. Sharing and sale of data

GVASTETHECREATOR does not sell Waller data.

Waller does not intentionally share local wallpaper images, paths, presets, monitor configuration, or settings with GVASTETHECREATOR or third parties.

## 6. Diagnostics and support

Users may voluntarily share screenshots, logs, preset files, or other diagnostic material when requesting support. Those materials may reveal wallpaper images, local paths, monitor names, or preset names.

Review and redact support material before sharing it publicly. Do not attach confidential images or sensitive local paths to a public issue.

## 7. Data retention and deletion

Waller keeps local settings and presets until the user removes them or Windows removes the package data.

Rendered files may remain in `%USERPROFILE%\.waller\rendered` so Windows can continue using the applied wallpaper and so updates do not unexpectedly break the current desktop. Users can delete unused rendered files when they are no longer referenced by Windows.

Uninstall behavior must be reviewed for each release. Waller must not silently delete original user-selected images. Original files remain owned and controlled by the user.

## 8. Security

Waller uses standard Windows and .NET APIs and is designed to operate without administrator privileges for normal use. No software can guarantee absolute security.

Report security issues privately according to the repository security instructions, without publishing private images, paths, or other sensitive data.

## 9. Children's privacy

Waller is a desktop customization utility and is not directed to children. It does not knowingly collect personal information from children.

## 10. Changes to this policy

This policy may be updated if Waller changes its storage, network, telemetry, wallpaper-source, or support behavior. Material changes will update this document and its effective date.

## 11. Contact

Privacy and support questions can be submitted through the public project channels without including confidential images, private file paths, or other sensitive content.

Repository: `gvastethecreator/waller`

---

# Política de Privacidad de Waller

**Fecha de vigencia:** 17 de agosto de 2026  
**Publicador:** debe completarse con el nombre verificado de Microsoft Store antes de la submission

Waller es un gestor local de fondos de pantalla para configuraciones de múltiples monitores en Windows. Permite seleccionar imágenes locales o colores, configurar la colocación por monitor, guardar presets, renderizar archivos y aplicarlos mediante APIs de escritorio de Windows.

Esta política explica qué información consulta Waller, qué guarda y qué no transmite.

## 1. Datos que Waller puede consultar

Waller puede consultar en el dispositivo:

- identificadores, límites, orientación y topología de monitores conectados;
- la ruta del fondo actual informada por Windows para cada monitor;
- imágenes locales seleccionadas explícitamente por el usuario;
- colores y opciones de colocación;
- nombres y configuración de presets;
- ajustes como idioma y apariencia;
- imágenes temporales o renderizadas para aplicar la composición;
- errores operativos necesarios para explicar por qué falló un archivo o una operación de Windows.

Waller utiliza esta información únicamente para las funciones de preview, presets, renderizado y aplicación del fondo.

## 2. Almacenamiento local

La aplicación empaquetada guarda presets y ajustes en el área local del paquete de Windows. La ruta efectiva incluye:

```text
%LOCALAPPDATA%\Packages\<package-family-name>\LocalCache\Local\Waller
```

Los PNG renderizados se guardan en una ubicación del perfil que puede leer el shell:

```text
%USERPROFILE%\.waller\rendered
```

Esta ubicación es necesaria para que las APIs de fondos de Windows lean los resultados fuera del almacenamiento virtualizado del paquete.

Los registros locales pueden contener rutas de imágenes, asignaciones de monitores, colocación, nombres de presets y preferencias. Se recomienda evitar información sensible en nombres de presets o rutas si luego pueden aparecer en capturas o material de soporte.

## 3. Red y transmisión

Waller no requiere una cuenta.

Waller no está diseñado para subir imágenes originales o renderizadas, rutas, información de monitores, presets ni ajustes a GVASTETHECREATOR o a otro servicio.

Las siguientes actividades pueden involucrar software externo:

- distribución, licencias, análisis de fallos y actualizaciones operados por Microsoft Store;
- enlaces de web, repositorio, documentación o soporte que el usuario decida abrir;
- rutas de imágenes de red elegidas y administradas por el usuario mediante Windows u otra aplicación.

Waller no ofrece un servicio cloud de wallpapers en esta versión.

## 4. Información personal

Waller no recopila intencionalmente nombres, correos, credenciales, identificadores publicitarios, contactos, historial de navegación, prompts, conversaciones ni contenido de documentos.

Las rutas locales y nombres de presets pueden contener indirectamente información elegida por el usuario. Waller mantiene esos valores en el dispositivo y los utiliza únicamente para el flujo local solicitado.

## 5. Cesión o venta

GVASTETHECREATOR no vende datos de Waller.

Waller no comparte intencionalmente imágenes, rutas, presets, configuración de monitores ni ajustes con GVASTETHECREATOR o terceros.

## 6. Diagnóstico y soporte

El usuario puede compartir voluntariamente capturas, logs, presets u otro material al solicitar soporte. Ese material podría mostrar imágenes, rutas, nombres de monitor o presets.

Debe revisarse y redactarse antes de publicarlo. No se deben adjuntar imágenes confidenciales ni rutas sensibles a un issue público.

## 7. Conservación y eliminación

Waller conserva presets y ajustes hasta que el usuario los elimina o Windows remueve los datos del paquete.

Los archivos renderizados pueden permanecer en `%USERPROFILE%\.waller\rendered` para que Windows continúe usando el fondo aplicado y una actualización no rompa el escritorio actual. El usuario puede eliminar resultados que ya no estén referenciados.

El comportamiento de desinstalación debe revisarse en cada release. Waller no debe borrar silenciosamente las imágenes originales seleccionadas por el usuario.

## 8. Seguridad

Waller utiliza APIs estándar de Windows y .NET y está diseñado para uso normal sin privilegios administrativos. Ningún software puede garantizar seguridad absoluta.

Los problemas de seguridad deben reportarse de forma privada según las instrucciones del repositorio, sin publicar imágenes, rutas ni datos sensibles.

## 9. Privacidad de menores

Waller es una utilidad de personalización de escritorio y no está dirigida a menores. No recopila conscientemente información personal de menores.

## 10. Cambios

Esta política puede actualizarse si cambian el almacenamiento, red, telemetría, fuentes de wallpaper o soporte. Los cambios materiales actualizarán este documento y la fecha de vigencia.

## 11. Contacto

Las consultas de privacidad y soporte pueden enviarse por los canales públicos del proyecto sin incluir imágenes confidenciales, rutas privadas ni otro contenido sensible.

Repositorio: `gvastethecreator/waller`
