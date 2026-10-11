# Registro de validación del Módulo 1

Verificación realizada el 10 de octubre de 2026 en Windows con Visual Studio Community 2026, MSBuild de Visual Studio, referencias de .NET Framework 4.8 e IIS Express. No se compiló ni ejecutó un proyecto .NET 10.

## Compilación

Se compiló el `.csproj` en Debug con `/t:Build /p:Configuration=Debug /p:MvcBuildViews=true` y luego en Release con `/p:Configuration=Release`, también con `MvcBuildViews=true`. Ambas compilaciones terminaron con código 0 y generaron `bin/legacyGestionDeFacturacion.web.dll`. La comprobación incluyó las vistas Razor. Las pruebas HTTP usaron la configuración local de desarrollo; no se aplicó una transformación de publicación ni se verificó hosting de producción.

El framework sigue siendo `v4.8` en `.csproj` y `4.8` en `Web.config`; MVC continúa en 5.2.9 y Razor/WebPages en 3.2.9. Se conservaron las 18 entradas de `packages.config`. No se incorporaron dependencias ni tecnologías de módulos posteriores.

## Verificaciones HTTP ejecutadas

Se inició una instancia local de IIS Express en `http://localhost:60121/` y se enviaron peticiones HTTP con un cliente PowerShell/.NET que conservó cookies y extrajo tokens desde los formularios. Se verificaron 31 condiciones:

| Área | Resultado observado |
| --- | --- |
| Inicio y listado | HTTP 200, título del sistema, clientes iniciales y estados activos/inactivos. |
| Detalle y edición GET | Detalle inicial correcto; IDs inexistentes o ausentes producen 404. |
| Alta | POST válido redirige al detalle; ID asignado por el servidor y cliente activo aunque se envíen campos Id/Activo adicionales. |
| Validación de alta | Campos vacíos o nombre con espacios y email inválido generan errores del servidor y devuelven el formulario. |
| Persistencia entre peticiones | El cliente creado se obtiene desde detalle y listado en peticiones posteriores. |
| Edición | POST válido actualiza los datos y conserva el estado; POST inválido vuelve al formulario sin modificar el almacenamiento. |
| Desactivación | PATCH real, POST tradicional y POST con override PATCH redirigen y dejan al cliente inactivo. |
| Idempotencia y errores | Repetir la desactivación conserva el estado; ID inexistente devuelve 404; GET no modifica datos. |
| Antiforgery | Alta y edición sin token se rechazan; PATCH sin token y POST con token pero sin su cookie se rechazan sin desactivar. |
| Estado durante edición | Editar un cliente activo no lo desactiva; editar uno inactivo no lo reactiva, aunque se intente enviar Activo. |
| Identificadores | Dos altas sucesivas recibieron IDs diferentes y crecientes. |
| Recursos | jQuery, jQuery Validation, unobtrusive y Bootstrap se sirven con HTTP 200. |

Durante la primera prueba, PATCH real alcanzó el handler StaticFile y devolvió 404 de IIS. Se añadió PATCH al handler de URLs sin extensión en `Web.config`; la suite completa pasó luego de esa corrección. Los rechazos antiforgery se comprobaron como respuestas de error y ausencia de modificación; no se promete un código 403 específico de MVC 5.

## Límites y comprobación manual

La restauración desde Internet no se ejecutó: los paquetes ya estaban presentes. Tampoco se realizó inspección visual en navegador, ejecución de JavaScript de validación, pruebas de carga ni una simulación de múltiples procesos. La sincronización se revisó en código; la suite HTTP comprueba IDs sucesivos, no un escenario de estrés concurrente.

Para completar una revisión visual en Visual Studio/IIS Express:

1. Abrir `/`, usar el enlace Clientes y comprobar el menú en ventana pequeña.
2. Crear un cliente, comprobar el detalle y volver al listado; editarlo y desactivarlo.
3. Enviar campos vacíos y email inválido, con y sin JavaScript, para observar mensajes de cliente y servidor.
4. Inspeccionar Network: el formulario de desactivación transporta POST con el campo de override PATCH y token antiforgery.
5. Abrir otra pestaña y confirmar que comparte los cambios. Reiniciar la aplicación y comprobar que reaparecen los tres ejemplos iniciales.

Los datos creados durante la validación pertenecen solo a la instancia en memoria de prueba. El código mantiene esa limitación deliberada; no existe una base de datos ni almacenamiento persistente.
