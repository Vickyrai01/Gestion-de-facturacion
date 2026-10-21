# Inyección de dependencias integrada

El Módulo 3 ya separa `FacturasController` de EF: recibe `IServicioFacturacion`, cuyo `ServicioFacturacion` recibe `IRepositorioFacturas`, `IConsultaClientes` e `IConsultaProductos`. `legacy/legacyGestionDeFacturacion.web/Composicion/FacturacionWeb.cs` construye los adaptadores con un contexto por petición y lo libera al disponer el controller. ClientesController y ProductosController conservan acceso directo a EF6.

Se reutiliza ese patrón de composición manual en el Windows Service, sin contenedor adicional:

- `Program.MostrarProductosAsync` crea un contexto, recibe la consulta a través de `IConsultaProductos` y usa `ConsultaProductosEf6`, con el mismo `ProductoConsultaDto` del Módulo 3. Mantiene el contexto vivo hasta finalizar el await.
- `ComposicionProcesos.ContarPendientes` crea un contexto por ciclo y usa `IRepositorioFacturas` / `RepositorioFacturasEf6`. No captura un DbContext compartido entre ticks.
- `ResumenPendientes(Func<int> contarPendientes, Serilog.ILogger log)` recibe la operación y el logger por constructor. La función permite componer el ciclo de vida del repositorio fuera del timer sin otra interfaz equivalente.
- Su constructor por defecto selecciona esa composición y posee el logger que creó. Un logger inyectado sigue siendo propiedad del llamador.

La consulta SQL de conteo tiene una sola implementación en el repositorio; `FacturacionOperaciones.ContarPendientes` delega en él como fachada de compatibilidad.

DI es pasar la dependencia desde afuera; no requiere un contenedor. Ejemplo compatible con Framework, para una sesión C# o un arnés referenciando el ejecutable y Serilog:

```csharp
using (var log = new Serilog.LoggerConfiguration().CreateLogger())
using (var proceso = new FacturacionLegacy.WindowsService.ResumenPendientes(() => 3, log))
{
    bool correcto = proceso.EjecutarUnaVez(); // true, sin conectar a SQL
}
```

Con un sink File o en memoria se pueden observar las propiedades. Para error controlado usar `() => { throw new InvalidOperationException("demo"); }`: devuelve false y registra TipoError sin el mensaje. El arnés `legacy/tools/ValidarModulo3.ps1` incorpora estas verificaciones y las de consultas async, además de los dobles del Módulo 3 que ejercitan sus interfaces.

## Contenedor nativo en .NET 10

Material educativo exclusivo para el futuro proyecto independiente; no se compila ni se instala en legacy. El host de ASP.NET Core integra registro, resolución y disposición de servicios. `AddTransient` crea por resolución; `AddScoped`, por scope (normalmente solicitud HTTP); `AddSingleton`, una instancia para el contenedor. Los contextos suelen ser scoped. MVC 5 dispone de DependencyResolver, pero esta aplicación usa composición manual; no incorpora el Generic Host ni los registros nativos de ASP.NET Core.

```csharp
// Boceto educativo .NET 10: usar los adaptadores del futuro proyecto moderno.
builder.Services.AddScoped<IConsultaProductos, ConsultaProductosModerna>();
```

`ConsultaProductosModerna` no existe en legacy. El host moderno resolverá parámetros de handlers/constructores y suministrará `ILogger<T>`. Un singleton no debe capturar un contexto scoped; en un job hospedado se crea un scope por ciclo. Ver [DI en .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview).
