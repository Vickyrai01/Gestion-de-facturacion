# Candidatos para Minimal APIs

No hay Minimal APIs ni rutas `/api` nuevas en legacy. Los endpoints siguientes son destinos propuestos para el proyecto independiente .NET 10. La aplicación actual usa MVC 5 y WCF SOAP.

| Consulta actual | Implementación real | Respuesta actual | Destino propuesto |
| --- | --- | --- | --- |
| GET `/Productos` (también `/Productos/Index`) | `legacy/legacyGestionDeFacturacion.web/Controllers/ProductosController.cs`, Index: EF6, AsNoTracking, OrderBy(Codigo), ToList | Vista HTML con los productos; entidades con Id, Codigo, Nombre, Descripcion, PrecioUnitario, Stock, Activo | GET `/api/productos`, JSON con DTO acotado y paginación definida |
| GET `/Facturas/Details/{id}` | `legacy/legacyGestionDeFacturacion.web/Controllers/FacturasController.cs`, Details → `IServicioFacturacion.Obtener` → `RepositorioFacturasEf6.Obtener` en `legacy/FacturacionLegacy.Datos/Facturacion/Infraestructura/RepositorioFacturasEf6.cs` | Vista HTML con FacturaDetalleDto (campos explícitos de cliente y líneas); 404 si no existe | GET `/api/facturas/{id}`, JSON con DTO de consulta |

Ambos controllers requieren `Permiso` para ADMIN u OPERADOR. El routing convencional se configura en `legacy/legacyGestionDeFacturacion.web/App_Start/RouteConfig.cs`. Productos conserva vistas con entidades; las vistas de Facturas usan los DTOs del Módulo 3. No hay un contrato JSON equivalente que pueda suponerse compatible.

Para la factura también existe `legacy/FacturacionLegacy.Wcf/FacturaConsultaService.svc.cs`, operación SOAP `ObtenerFacturaPorId`, que conserva la fachada FacturacionOperaciones.Obtener y proyecta `FacturaConsulta`: Id, Numero, ClienteId, FechaEmision UTC, Estado, Subtotal, Impuestos, Total y Detalles (ProductoId, Codigo, Cantidad, PrecioUnitario, Subtotal). Sus contratos están en `Contratos.cs`. Usa faults controlados y autorización local configurada para WCF; migrarlo requiere decidir el contrato HTTP y permisos, no copiar el acceso local como autorización pública.

## Boceto educativo para .NET 10, no compilado en legacy

Este fragmento Markdown no es una aplicación completa. `IConsultaProductos.ListarAsync` y `IServicioFacturacion.Obtener` representan los contratos reales integrados. El primero es async; la consulta de factura sigue siendo síncrona. En el futuro proyecto habrá que implementar/adaptar esos contratos para su runtime; no se añade una referencia al ejecutable del Windows Service. Presupone registros en `builder.Services` y autenticación/autorización del futuro host. `LecturaFacturacion` debe requerir usuario autenticado y roles ADMIN u OPERADOR. No reutilizar la cookie legacy sin un diseño de interoperabilidad.

```csharp
app.MapGet("/api/productos", async (
    IConsultaProductos consultas, CancellationToken ct) =>
{
    var productos = await consultas.ListarAsync(ct);
    return Results.Ok(productos);
}).RequireAuthorization("LecturaFacturacion");

app.MapGet("/api/facturas/{id:int}", (
    int id, IServicioFacturacion servicio) =>
{
    if (id <= 0) return Results.BadRequest();
    var factura = servicio.Obtener(id);
    if (factura is null) return Results.NotFound();
    // Proyección educativa: no publicar ClienteDocumento/nombres del DTO completo.
    return Results.Ok(new { factura.Id, factura.Numero, factura.Estado,
        factura.Subtotal, factura.Impuestos, factura.Total });
}).RequireAuthorization("LecturaFacturacion");
```

Se propone 400 para id no positivo y 404 para factura ausente; una ruta no compatible con `int` no coincide. El token de productos representa la cancelación de la solicitud. La factura conserva la firma síncrona real; propagar async en ese recorrido requiere otro cambio de contratos, no envolverlo en Task.Run. Definir paginación/límites para productos y DTO con campos aprobados; el límite fijo de 20 de la consola es didáctico, no contrato definitivo. No exponer navegaciones EF, datos de clientes o entidades completas accidentalmente.

En ASP.NET Core las rutas se declaran con `MapGet`, los parámetros se enlazan desde ruta/DI y el host usa `Program` para servicios y middleware. En MVC 5 hay acciones que devuelven `ActionResult`, routing convencional en App_Start y configuración System.Web/Web.config. La Minimal API devuelve resultados HTTP/JSON; no reemplaza por sí sola la vista ni la UI, y SOAP cambia a un contrato distinto. El acceso a datos debe seguir detrás de los servicios acordados, sin SQL dentro del handler. Ver [referencia oficial de Minimal APIs .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0).
