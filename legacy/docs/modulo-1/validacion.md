# Validación del Módulo 1

Esta práctica permite observar el comportamiento del ejemplo inicial con almacenamiento en memoria y compararlo con la persistencia EF6 de la aplicación actual.

## Compilación

En Windows con Visual Studio/MSBuild y .NET Framework 4.8, compilar Debug y Release con `MvcBuildViews=true` para incluir las vistas Razor. Comprobar el framework de destino y la compatibilidad de MVC, Razor y las dependencias antes de considerar una migración.

## Escenarios HTTP del ejemplo inicial

Ejecutar con IIS Express y conservar cookies y tokens antiforgery al enviar formularios. Comprobar los siguientes resultados:

| Área | Resultado esperado |
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
| Identificadores | Dos altas sucesivas reciben IDs diferentes y crecientes. |
| Recursos | jQuery, jQuery Validation, unobtrusive y Bootstrap se sirven con HTTP 200. |

En IIS, PATCH debe estar habilitado en el handler de URLs sin extensión de Web.config. Los rechazos antiforgery deben impedir la modificación de datos; no asumir un código 403 específico de MVC 5.

## Comprobación manual

Las peticiones HTTP no ejecutan JavaScript ni validan el diseño visual. Para estudiar concurrencia, agregar pruebas específicas: dos IDs sucesivos distintos no demuestran seguridad bajo carga.

Para completar una revisión visual en Visual Studio/IIS Express:

1. Abrir `/`, usar el enlace Clientes y comprobar el menú en ventana pequeña.
2. Crear un cliente, comprobar el detalle y volver al listado; editarlo y desactivarlo.
3. Enviar campos vacíos y email inválido, con y sin JavaScript, para observar mensajes de cliente y servidor.
4. Inspeccionar Network: el formulario de desactivación transporta POST con el campo de override PATCH y token antiforgery.
5. Abrir otra pestaña y confirmar que comparte los cambios. Reiniciar la aplicación y comprobar que reaparecen los tres ejemplos iniciales.

En el ejemplo inicial, los datos pertenecen a la instancia en memoria y se reinician con la aplicación. En la versión con EF6 del Módulo 2, comprobar en cambio que los datos persisten tras el reinicio.
