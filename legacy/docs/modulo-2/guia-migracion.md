# Guía conceptual para las clases 3, 4 y 5

El repositorio es la referencia legacy. Los alumnos crearán otro proyecto .NET 10 en `modern/`, manteniendo esta rama disponible para comparar resultados. No hay código moderno implementado aquí. Todas las rutas de código citadas a continuación son relativas a `legacy/`. Los documentos del Módulo 1 describen su versión previa; para ejecutar esta rama usar el README del Módulo 2.

## Clase 3: MVC y persistencia

### MVC 5 hacia ASP.NET Core MVC

Situación actual: el sitio utiliza `System.Web.Mvc` 5.2.9, Razor 3.2.9, Global.asax, RouteConfig, FilterConfig, bundles y Web.config. No hay páginas WebForms `.aspx`: el ejemplo real de este repositorio es MVC. Si se compara WebForms en clase, separar ese caso conceptual del código existente.

Archivos relevantes: `legacyGestionDeFacturacion.web/Controllers/ClientesController.cs`, `ProductosController.cs`, `FacturasController.cs`, `Views/Clientes/`, `Views/Productos/`, `Views/Facturas/`, `App_Start/` y `Global.asax.cs`. Revisar `Index`, `Create`, `Edit`, `Desactivar`, `Emitir` y `Anular`.

Compatibilidad: los tipos y el pipeline System.Web no se trasladan directamente a ASP.NET Core. Cambian filtros, helpers, configuración, routing y compilación de vistas; también hay que conservar el override PATCH y antiforgery. Dependencias actuales y versiones están en `packages.config`. La alternativa conceptual es ASP.NET Core MVC y Razor, según la [guía oficial de MVC clásico a Core](https://learn.microsoft.com/en-us/aspnet/core/migration/fx-to-core/examples/overview?view=aspnetcore-10.0).

Pasos sugeridos: inventariar URLs/verbos/permisos, crear el sitio paralelo, reproducir la ruta convencional, adaptar un controller y su vista, conectar sus consultas y verificar comportamiento antes de repetir con el resto. Validar errores de campos, IDs inexistentes, 403, generación de enlaces y desactivación POST/PATCH. No introducir una SPA para reproducir este ejercicio.

### EF6 hacia EF Core

Situación actual: biblioteca `FacturacionLegacy.Datos`, paquete EntityFramework 6.5.1, SQL Server y Code First. Archivos: `FacturacionDbContext.cs`, `Entidades/*.cs`, `Migraciones/` y `FacturacionOperaciones.cs`. `Consultar` compone filtros; `Obtener` carga detalles; `Crear` realiza validación/cálculos/transacción y obtiene la secuencia SQL.

Problemas a revisar: APIs/fluent mapping, traducción de LINQ, Include, seguimiento, precisión, transacciones y migraciones. EF6 tiene assets para .NET moderno: esto no prueba que cada uso funcione sin cambios ni obliga a migrar el ORM junto con el runtime. EF Core tiene otra implementación y su historial de migraciones no es intercambiable. Ver [portabilidad de EF6 a EF Core](https://learn.microsoft.com/en-us/ef/efcore-and-ef6/porting/).

Pasos sugeridos: elegir una base de comparación separada, reproducir tablas/relaciones/índices/checks, definir una estrategia de esquema e historial, migrar primero la consulta de pendientes y luego las de facturas, comparar SQL/resultados y finalmente crear facturas con el mismo contrato. Validar 551.00 de subtotal, 115.71 de impuesto y 666.71 de total para precios 125.50×2 y 100×3 a tasa 0.21. Probar atomicidad, precios históricos y altas concurrentes sin descontar stock.

## Clase 4: integración y procesos

### WCF hacia REST o gRPC

Situación actual: `FacturacionLegacy.Wcf`, referencias de Framework a System.ServiceModel/System.Runtime.Serialization y acceso a EF6 vía Datos. Archivos: `Contratos.cs`, `FacturaConsultaService.svc`, `.svc.cs`, `SoloLocalAuthorizationManager.cs` y `Web.config`. Contrato SOAP `IFacturaConsultaService` con tres operaciones de consulta, DTOs y faults.

Compatibilidad: el hosting WCF clásico y SOAP/WSDL no se sustituyen cambiando una referencia a ASP.NET Core. REST cambia el contrato HTTP y gRPC requiere definir mensajes/servicios protobuf; clientes, errores, seguridad y serialización se revisan explícitamente. La alternativa elegida para el primer ejercicio será una API REST de consultas; gRPC se compara conceptualmente.

Pasos sugeridos: capturar solicitudes/respuestas SOAP, definir recursos y códigos HTTP, mapear DTOs/errores y crear un consumidor equivalente en el proyecto nuevo. Para gRPC, analizar representación de decimal y fechas y compatibilidad del consumidor. Verificar los tres filtros, factura inexistente, estado inválido, ausencia de grafos EF y acceso autorizado. No exponer el servicio remoto copiando el permiso loopback de la demostración.

### Windows Service hacia Worker Service

Situación actual: `FacturacionLegacy.WindowsService`, ServiceBase y System.Threading.Timer; referencia a la biblioteca Datos y EF6, sin referencia al MVC. Archivos: `Program.cs`, `FacturacionService.cs`, `ResumenPendientes.cs` y `App.config`. El núcleo es `EjecutarUnaVez` y la consulta `ContarPendientes`.

Compatibilidad: ServiceBase/SCM, configuración y logging se revisan por separado del acceso a datos. Un Worker Service con Generic Host/BackgroundService utiliza otro ciclo de vida y cancelación, como describe [Worker Services](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers).

Pasos sugeridos: conservar el resumen como operación de solo lectura, adaptar consulta/configuración/logs, implementar ejecución periódica con cancelación, seleccionar alojamiento y cuenta del proceso. Verificar intervalos, dos ciclos, detención durante una consulta, liberación del contexto y ausencia de cambios de estado/datos personales en logs. Comprobar permisos SQL y de archivos con la identidad real; LocalDB del alumno no representa todos los despliegues del servicio.

## Clase 5: autenticación y verificación conjunta

### Forms Authentication hacia un mecanismo moderno

Situación actual: System.Web.Security.FormsAuthentication y Authorize; Usuario EF6 y BCrypt.Net-Next con dependencias System.Memory/Buffers/Vectors/Unsafe. Archivos: `AccountController.cs`, `Models/LoginModel.cs`, `Views/Account/Login.cshtml`, `Seguridad/PermisoAttribute.cs`, `IntentosLogin.cs`, `Global.asax.cs`, `Web.config`, `Datos/PasswordSeguridad.cs` y `Tools/Program.cs`.

Problemas potenciales: cookies/tickets Forms no son cookies de Core; hay que diseñar equivalencia de roles, migración de hashes, cierre de sesión, revocación y controles de abuso. Las llamadas bcrypt pueden conservarse si sus assets/APIs se validan; no se declara incompatibilidad por el nombre legacy. La alternativa habitual es autenticación por cookies de Core con Identity, o un proveedor de identidad con OpenID Connect/OAuth según el escenario. Ver [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0).

Pasos sugeridos: definir identidad y permisos, decidir cómo mover/verificar hashes existentes, crear un login seguro y volver a emitir tickets, reproducir controles de servidor y probar cuentas activas/inactivas. Verificar login/logout, returnUrl externo rechazado, antiforgery, HTTPS, mensajes genéricos y matriz ADMIN/OPERADOR. No copiar credenciales de desarrollo a otro entorno ni habilitar registro público sin un requisito nuevo.

## Comparación conjunta

El script `tools/ValidarModulo2.ps1` proporciona casos HTTP, SQL y SOAP de referencia. Adaptar su intención al proyecto moderno: la suite evalúa los resultados observables, no exige repetir los detalles internos del legacy. Registrar por separado compilación, validación funcional, integración y aspectos no ejecutados.
