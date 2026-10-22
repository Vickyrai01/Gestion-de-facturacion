# Módulo 4 — Modernización de código .NET

Este módulo presenta async/await, inyección de dependencias, logging estructurado y procesamiento en segundo plano sobre la arquitectura del sistema de facturación. Los ejemplos ejecutables utilizan .NET Framework 4.8, ASP.NET MVC 5 y Entity Framework 6; la comparación con .NET 10 se desarrolla como material educativo.

| Tema | Ejemplo legacy | Comparación con .NET 10 |
| --- | --- | --- |
| Async/await | EF6: listado acotado y detalle de productos por consola | Propagación de async en el recorrido HTTP |
| Logging | Serilog con propiedades y archivos JSON | `ILogger<T>` y configuración del host .NET 10 |
| Background jobs | Timer, exclusión mutua y parada del Windows Service | `BackgroundService` / `IHostedService` |
| DI | Interfaces compartidas y operación inyectada en el resumen | Contenedor nativo en ASP.NET Core |
| Minimal APIs | Auditoría y ejemplos solo en Markdown | Proyecto .NET 10 independiente |
| Clean Architecture | Propuesta de límites y responsabilidades | Contratos y adaptadores independientes del anfitrión |

## Recorrido

Leer [async-await](async-await.md), [logging](logging.md), [background-jobs](background-jobs.md), [dependency-injection](dependency-injection.md), [minimal-apis](minimal-apis.md) e [arquitectura aplicada a la modernización](integracion-modulo-3.md). Toda la documentación de los módulos está dentro de `legacy/docs/`.

En Windows, con Visual Studio/MSBuild compatible con `.slnx`, Developer Pack 4.8 y SQL Server/LocalDB, desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\RestaurarPaquetes.ps1
msbuild .\legacy\SistemaFacturacion.Legacy.slnx /p:Configuration=Debug /p:MvcBuildViews=true
```

Preparar la base según `legacy/docs/modulo-2/entity-framework.md`. Configurar `FacturacionDb` en `legacy/FacturacionLegacy.WindowsService/App.config` antes de compilar, o en el `.exe.config` para ejecutar. `FACTURACION_DB_CONNECTION` permite una base de pruebas independiente. Los ejemplos de este módulo solo leen; no ejecutar migraciones contra una base ajena para demostrarlo.

```powershell
$exe = '.\legacy\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe'
& $exe --productos-async
& $exe --productos-async 1
& $exe --una-vez
& $exe --consola --segundos 12
```

Para dos o más ciclos en 12 segundos, ajustar `IntervaloSegundos=5` en el `.exe.config`. Al compilar nuevamente se copia App.config. Los logs quedan relativos al ejecutable salvo `LogDirectory` absoluto.

## Archivos y alcance

Las lecturas async están en `legacy/FacturacionLegacy.Datos/Productos/ConsultaProductosEf6.cs`, usan `IConsultaProductos` y devuelven `ProductoConsultaDto`. El conteo está en `IRepositorioFacturas` / `RepositorioFacturasEf6`; la fachada `FacturacionOperaciones.ContarPendientes` conserva su firma y delega allí.

`legacy/FacturacionLegacy.WindowsService/ComposicionProcesos.cs` crea el contexto por ciclo; `ResumenPendientes` recibe la operación mediante `Func<int>` y el logger. `Program` compone la consulta de productos con un contexto que dura hasta terminar el await. Se mantiene el patrón de composición manual de `FacturacionWeb`, sin contenedores adicionales.

El logging utiliza Serilog 2.12.0 y Serilog.Sinks.File 5.0.0, compatibles con .NET Framework 4.8. El arnés de arquitectura utiliza `legacy/tools/pruebas-modulo-3/PruebasIntegracionModulo4.cs` para comprobar consultas con proveedor EF6 en memoria y el resumen con logger en memoria.

## Práctica de validación

Realizar las siguientes comprobaciones en Windows con los requisitos indicados:

1. Restaurar y compilar Debug y Release, incluyendo Razor (`MvcBuildViews=true`).
2. Comparar los primeros 20 Id/Codigo/PrecioUnitario con SQL ordenado por Codigo, Id. Probar detalle existente, inexistente, id inválido y Ctrl+C. Verificar salidas 0/1/2 según caso. Una consulta rápida puede terminar antes de cancelar; probar también un token ya cancelado invocando el método directamente.
3. Ejecutar `--una-vez`; leer JSON con `Get-Content ... | ConvertFrom-Json`: inicio, cantidad numérica y finalización con duración. Comparar cantidad con SQL. Probar conexión inválida en una base de pruebas: salida 1 y `TipoError`, sin cadena de conexión ni mensaje SQL.
4. Ejecutar consola con intervalo 5 y duración 12, luego Ctrl+C en modo sin límite. Confirmar varios ciclos, ausencia de solapamiento y que no aparecen eventos nuevos tras salir. Comparar estados/cantidades de facturas antes/después.
5. Ejecutar `legacy/tools/ValidarModulo2.ps1` para regresión MVC/WCF/facturación; se conserva la línea `Facturas pendientes:` en un `.log` para sus comprobaciones. Revisar visualmente MVC e instalar opcionalmente el servicio con la cuenta correcta según la guía del Módulo 2.
6. Ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1`. Además de las pruebas de arquitectura, compila y ejecuta las verificaciones sin SQL: orden/límite de listado async, inactivos, detalle ausente, id inválido, token cancelado, conservación de consultas síncronas, logging numérico, parada y error sin mensaje sensible. El proveedor en memoria no valida traducción SQL ni cancelación en vuelo; realizar también los pasos anteriores con SQL real. Probar una consulta lenta para exclusión/parada.
