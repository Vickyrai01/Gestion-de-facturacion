# Async / await con EF6

## Punto de partida real

`legacy/legacyGestionDeFacturacion.web/Controllers/ProductosController.cs`, `Index`, ejecuta `db.Productos.AsNoTracking().OrderBy(p => p.Codigo).ToList()`. `Details` usa `Find`. Después del Módulo 3, `FacturasController.Index` delega en `IServicioFacturacion.Consultar`, que usa `IRepositorioFacturas`; las consultas del repositorio siguen siendo síncronas. `FacturacionOperaciones.Obtener` es una fachada que delega en `RepositorioFacturasEf6.ObtenerEntidad` (SingleOrDefault). El hilo espera la respuesta de SQL en estos recorridos; la integración conserva sus firmas y comportamiento.

La demostración está integrada en `legacy/FacturacionLegacy.Datos/Productos/ConsultaProductosEf6.cs`, detrás de `IConsultaProductos`. Reutiliza `ProductoConsultaDto` del Módulo 3 (Id, Codigo, Nombre, PrecioUnitario, Activo), sin un segundo DTO. `ListarAsync` ordena por Codigo/Id, limita a 20 y materializa con `ToListAsync(token)`; `ObtenerAsync` usa `FirstOrDefaultAsync` por clave primaria. Incluye productos activos e inactivos como el listado original. La proyección y el límite son decisiones de la demo; no equivalen a todos los campos/filas de la vista MVC.

```csharp
// Forma síncrona equivalente de la consulta acotada (comparación educativa):
var productos = db.Productos.AsNoTracking()
    .OrderBy(p => p.Codigo).ThenBy(p => p.Id).Take(20).ToList();
// Fragmento del recorrido implementado, después de proyectar el DTO:
return await Proyectar(db.Productos.AsNoTracking()).OrderBy(p => p.Codigo).ThenBy(p => p.Id)
    .Take(20).ToListAsync(cancellationToken).ConfigureAwait(false);
```

`Program.MostrarProductosAsync` compone `IConsultaProductos` con `ConsultaProductosEf6`, fija timeout SQL de 30 segundos y posee el contexto hasta después del await. El adaptador recibe el contexto como el resto del Módulo 3 y no lo dispone. Otros consumidores deben respetar esa propiedad y no compartirlo entre operaciones concurrentes. Se importa `System.Data.Entity`, no EF Core. `SaveChangesAsync` ya existe en EF6, pero no corresponde a esta lectura y no se cambia la creación de facturas. Ver [documentación oficial de asincronismo EF6](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/async).

## Qué enseña

Una operación asíncrona devuelve un `Task` que representa su finalización. Durante una espera de I/O incompleta, `await` permite devolver el control al llamador; no crea por sí mismo un hilo. Paralelismo significa ejecutar trabajo simultáneamente. Aquí las consultas no corren en paralelo. Async no acelera el SQL ni elimina el costo de materializar resultados.

En una aplicación web puede liberar el hilo de la solicitud durante la espera y permitir atender otras solicitudes. La demo de consola muestra las APIs y cancelación; no mide escalabilidad web. MVC 5 también admite acciones `async Task<ActionResult>`. En una evolución del recorrido HTTP habrá que propagar async desde la acción hasta el proveedor, conservando permisos y la vista. Esta integración mantiene las acciones MVC síncronas; los métodos nuevos no sustituyen `ListarActivos` ni `Obtener(int[])`, usados para crear facturas.

ASP.NET MVC 5 tiene un contexto de sincronización asociado a la solicitud. ASP.NET Core normalmente no lo tiene; mantiene el modelo de Tasks, usa su pipeline y permite propagar `HttpContext.RequestAborted`. `ConfigureAwait(false)` en esta consulta independiente evita exigir volver a un contexto del llamador; no acceder luego a objetos de la solicitud en esa capa.

## Demostración

Desde la raíz, después de compilar y preparar SQL:

```powershell
$exe = '.\legacy\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe'
& $exe --productos-async
& $exe --productos-async 1
& $exe --productos-async 2147483647
& $exe --productos-async -1
```

Listado vacío: salida 0. Detalle inexistente/argumento inválido/error: salida 1. Ctrl+C solicita cancelación mediante `CancellationTokenSource`; `OperationCanceledException` devuelve 2. La cancelación depende también del proveedor SQL y puede no ser inmediata. El detalle imprime solo Id, código y precio; no se escribe en el log del resumen.

`Program.Main` usa `GetAwaiter().GetResult()` exclusivamente para conectar el Main síncrono legacy con el método async de consola. No copiar ese bloqueo a MVC. Evitar `.Result`, `.Wait()`, `async void`, olvidar await, liberar el contexto antes de terminar, compartir DbContext entre hilos o lanzar dos operaciones concurrentes sobre él. No usar `Task.Run` para envolver SQL síncrono ni convertir todas las operaciones de la aplicación sin analizar el recorrido.
