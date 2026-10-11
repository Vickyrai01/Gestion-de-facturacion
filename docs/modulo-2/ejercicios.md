# Ejercicios para las clases 3, 4 y 5

Trabajar en el proyecto .NET 10 independiente del curso. Esta rama queda como referencia; los ejercicios no incluyen soluciones modernas implementadas. Preparar la base y cuentas locales usando el README del Módulo 2 y emplear datos ficticios.

## 1. Migrar un controller MVC 5

Punto de partida: `legacyGestionDeFacturacion.web/Controllers/ClientesController.cs`, métodos `Index`, `Details`, `Create`, `Edit` y `Desactivar`; vistas `Views/Clientes/*.cshtml`, `App_Start/RouteConfig.cs` y `Seguridad/PermisoAttribute.cs`.

Registrar rutas y verbos, seguir una petición en el debugger y observar ModelState/antiforgery. En el proyecto paralelo, reproducir el listado y detalle antes del alta/edición. Conservar las URLs y la desactivación POST/PATCH. Explicar cómo se configura routing y dónde se aplican los roles en cada versión.

Criterios: ADMIN puede escribir y OPERADOR solo consultar; campos requeridos/email válido; ID inexistente devuelve 404; las escrituras rechazan ausencia de token; desactivar no elimina el registro. Presentar dos capturas de Network y los resultados de comparación, sin modificar el legacy.

## 2. Migrar una consulta de EF6 a EF Core

Punto de partida: `FacturacionLegacy.Datos/FacturacionOperaciones.cs`, `Consultar` y `Obtener`; `FacturacionDbContext.OnModelCreating` y entidades Factura/DetalleFactura.

Elegir una base de datos de pruebas distinta. Reproducir los mapeos y escribir la consulta por cliente y estado en el proyecto paralelo. Comparar resultados y SQL generado. Explicar cuándo se ejecuta IQueryable y por qué se usa AsNoTracking/Include. Identificar cómo conservar decimal(18,2), FK e índices únicos.

Criterios: mismos IDs/números/estados/importes, sin multiplicar filas por los detalles; detalle con precios históricos; resultado vacío cuando no hay coincidencias. Diseñar una estrategia de migraciones nueva sin ejecutar Down ni eliminar la base original. Como ampliación, reproducir la creación atómica y probar números únicos bajo concurrencia.

## 3. Reemplazar la consulta WCF con REST

Punto de partida: `FacturacionLegacy.Wcf/Contratos.cs` y `FacturaConsultaService.svc.cs`, operaciones `ObtenerFacturaPorId`, `ObtenerFacturasPorCliente` y `ObtenerFacturasPorEstado`; configuración `basicHttpBinding` en `Web.config`.

Consumir SOAP y registrar una respuesta válida y un fault. Diseñar recursos/rutas REST equivalentes, entradas, DTOs y códigos de error. Implementar ese contrato solo en el proyecto nuevo y preparar un consumidor que compare respuestas. Describir qué ocurriría con un consumidor existente que esperaba SOAP/WSDL.

Criterios: tres consultas equivalentes; factura inexistente y estado inválido tienen errores definidos; ninguna respuesta expone DbContext o navegaciones completas; importes/UTC tienen representación acordada; definir identidad y permisos para acceso remoto. Comparar REST con una posible definición gRPC sin agregar gRPC al legacy.

## 4. Migrar el Windows Service a Worker Service

Punto de partida: `FacturacionLegacy.WindowsService/ResumenPendientes.cs` (`EjecutarUnaVez`, `Iniciar`, `Dispose`), `FacturacionService.OnStart/OnStop` y `FacturacionOperaciones.ContarPendientes`.

Ejecutar `--una-vez` y `--consola --segundos 10`, inspeccionar los logs y el comportamiento al detener. En el proyecto paralelo, reproducir el resumen con Worker/BackgroundService, cancelación y configuración equivalentes. Seleccionar un intervalo pequeño para observar al menos dos ciclos.

Criterios: inicia sin bloquear el host; no superpone consultas; registra fecha/cantidad sin datos personales; no modifica facturas; detiene y libera recursos. Probar un error de conexión y documentar permisos SQL/archivos para una cuenta de servicio. Diferenciar consola de instalación real en SCM.

## 5. Reemplazar Forms Authentication

Punto de partida: `AccountController.Login/Logout`, `Global.asax.Application_PostAuthenticateRequest`, `PasswordSeguridad`, `PermisoAttribute` y `Web.config`. Usar las cuentas locales creadas por `FacturacionLegacy.Tools usuarios-demo`, sin colocar contraseñas en código o entregas.

Explicar cómo se protege el ticket Forms y cómo se obtiene el rol desde SQL. Diseñar un mecanismo del proyecto nuevo con cookies/Identity o identidad federada según el escenario acordado. Documentar qué hacer con los hashes bcrypt existentes y por qué no se pueden reutilizar automáticamente las cookies de System.Web.

Criterios: login/logout correctos, errores genéricos, cuentas inactivas rechazadas, HTTPS, antiforgery, redirecciones locales y matriz ADMIN/OPERADOR aplicada en el servidor. Probar acceso directo a `/Facturas/Emitir/{id}` con OPERADOR; ocultar botones por sí solo no completa el ejercicio.

## Entrega conjunta

Presentar un inventario de cambios, archivos equivalentes, contratos conservados, pruebas ejecutadas y pendientes. Usar como base `tools/ValidarModulo2.ps1` y `docs/modulo-2/guia-migracion.md`. Mantener datos históricos y separar problemas del runtime, framework web, ORM, integración y hosting; no declarar compatibilidad o incompatibilidad solo porque un paquete compila.
