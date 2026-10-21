# Inyección de dependencias

Hoy los controllers instancian `new FacturacionDbContext()` y llaman a `FacturacionOperaciones`, una clase estática. No hay contenedor nativo de ASP.NET Core dentro de MVC 5. El Módulo 3 está separando esas responsabilidades: aquí no se crean repositorios globales alternativos.

El ejemplo ejecutable está en `legacy/FacturacionLegacy.WindowsService/ConsultaPendientes.cs` y `ResumenPendientes.cs`:

- `IConsultaPendientes` declara `int Contar()`.
- `ConsultaPendientesEf` es la dependencia concreta: crea y libera un contexto por ejecución y delega en `FacturacionOperaciones.ContarPendientes`.
- `ResumenPendientes(IConsultaPendientes consulta, Serilog.ILogger log)` recibe dependencias por constructor. Coordina ejecución, errores y registro sin construir EF dentro de su operación.
- El constructor sin argumentos compone las implementaciones manualmente para conservar las llamadas de `Program` y `FacturacionService`. Solo dispone el logger que él mismo creó. El llamador debe disponer un logger inyectado.

DI es pasar la dependencia desde afuera; no requiere un contenedor. Esto permite sustituir SQL por una implementación controlada durante una clase o prueba. Ejemplo compatible con Framework, para una sesión C# o un arnés de pruebas referenciando el ensamblado y Serilog (no es otro proyecto agregado):

```csharp
class PendientesDemo : FacturacionLegacy.WindowsService.IConsultaPendientes
{
    public int Contar() { return 3; }
}
// Dentro del método de demostración:
using (var log = new Serilog.LoggerConfiguration().CreateLogger())
using (var proceso = new FacturacionLegacy.WindowsService.ResumenPendientes(new PendientesDemo(), log))
{
    bool correcto = proceso.EjecutarUnaVez(); // true, sin conectar a SQL
}
```

Para observar propiedades, crear el logger con el sink File configurado como en `RegistroProcesamiento`. Para enseñar error controlado, reemplazar el cuerpo por `throw new InvalidOperationException("demo");`: resultado false y `TipoError`, sin almacenar el mensaje.

## Contenedor nativo en .NET 10

Material educativo exclusivo para el futuro proyecto independiente; no se compila ni se instala en legacy. El host de ASP.NET Core integra registro, resolución y disposición de servicios. `AddTransient` crea por resolución; `AddScoped`, por scope (normalmente solicitud HTTP); `AddSingleton`, una instancia para el contenedor. Los contextos de datos suelen ser scoped. MVC 5 dispone de `DependencyResolver`, pero no de la integración del Generic Host ni de estos registros de manera nativa.

```csharp
// Esquema conceptual: tipos a definir/reconciliar en Módulo 3.
builder.Services.AddScoped<IConsultaProductos, ConsultaProductos>();
```

El host resuelve parámetros de handlers y constructores y suministra `ILogger<T>`. Un singleton no debe capturar el contexto scoped. En un job hospedado hay que crear un scope por ciclo. Ver [DI en .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview).

La integración final debe sustituir `IConsultaPendientes` por el contrato acordado del Módulo 3 y mover la composición al borde de la aplicación. Evitar dos interfaces equivalentes y mantener la propiedad/disposición del contexto y del logger explícita.
