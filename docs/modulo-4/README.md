# Módulo 4 — Modernización de código .NET

Base: `modulo-2-migracion-tecnica`. Trabajo aislado en `modulo-4-modernizacion`, sin integrar el Módulo 3, sin commits, pushes ni cambios en `modern/`. Los archivos de código agregados pertenecen al Windows Service existente y compilan para .NET Framework 4.8; no hay proyecto moderno.

| Tema | Demostración ejecutable ahora | Integración posterior |
| --- | --- | --- |
| Async/await | EF6: listado acotado y detalle de productos por consola | Contratos async y recorrido HTTP del Módulo 3 |
| Logging | Serilog con propiedades y archivos JSON | `ILogger<T>` y configuración del host .NET 10 |
| Background jobs | Timer, exclusión mutua y parada del Windows Service | `BackgroundService` / `IHostedService` |
| DI | Interfaz y constructor en el resumen de pendientes | Contenedor nativo en ASP.NET Core |
| Minimal APIs | Auditoría y ejemplos solo en Markdown | Proyecto .NET 10 independiente |
| Clean Architecture | Propuesta de límites y responsabilidades | Adaptación a los contratos del Módulo 3 |

## Recorrido

Leer [async-await](async-await.md), [logging](logging.md), [background-jobs](background-jobs.md), [dependency-injection](dependency-injection.md), [minimal-apis](minimal-apis.md) e [integracion-modulo-3](integracion-modulo-3.md). La documentación anterior está en `legacy/docs/modulo-2/`; las nuevas guías usan rutas relativas a la raíz del repositorio salvo indicación.

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

Nuevos: `ConsultaPendientes.cs`, `ConsultasProductosAsync.cs`, `RegistroProcesamiento.cs` en `legacy/FacturacionLegacy.WindowsService/`, y estos siete documentos. Modificados: `ResumenPendientes.cs`, `Program.cs`, el `.csproj` y `packages.config` de ese mismo proyecto. No se cambian controllers, EF6 compartido, WCF, vistas ni flujos de escritura. Serilog 2.12.0 y Serilog.Sinks.File 5.0.0 son las únicas dependencias nuevas; restaurarlas con el script existente. Los binarios descargados no se versionan.

## Validación y evidencia

Revisión realizada el 21/10/2026 en macOS: estado inicial limpio, base y rama de trabajo verificadas; inspección del código síncrono, timer, permisos MVC, contratos WCF y script de validación anterior. Se revisan XML, inclusiones del proyecto, rutas de documentación y diff. No hay `dotnet`, `mono`, `msbuild`, `nuget` ni `csc` en este entorno. No se ejecutaron compilación, restauración, SQL Server, MVC, consultas async, escritura de logs ni timer. La evidencia del Módulo 2 corresponde a aquella rama y no valida estos cambios.

Pendiente en Windows:

1. Restaurar y compilar Debug y Release, incluyendo Razor (`MvcBuildViews=true`).
2. Comparar los primeros 20 Id/Codigo/PrecioUnitario con SQL ordenado por Codigo, Id. Probar detalle existente, inexistente, id inválido y Ctrl+C. Verificar salidas 0/1/2 según caso. Una consulta rápida puede terminar antes de cancelar; probar también un token ya cancelado invocando el método directamente.
3. Ejecutar `--una-vez`; leer JSON con `Get-Content ... | ConvertFrom-Json`: inicio, cantidad numérica y finalización con duración. Comparar cantidad con SQL. Probar conexión inválida en una base de pruebas: salida 1 y `TipoError`, sin cadena de conexión ni mensaje SQL.
4. Ejecutar consola con intervalo 5 y duración 12, luego Ctrl+C en modo sin límite. Confirmar varios ciclos, ausencia de solapamiento y que no aparecen eventos nuevos tras salir. Comparar estados/cantidades de facturas antes/después.
5. Ejecutar `legacy/tools/ValidarModulo2.ps1` para regresión MVC/WCF/facturación; se conserva la línea `Facturas pendientes:` en un `.log` para sus comprobaciones. Revisar visualmente MVC e instalar opcionalmente el servicio con la cuenta correcta según la guía del Módulo 2.
6. Probar la inyección con un doble que devuelva 3 y otro que lance `InvalidOperationException`, sin SQL. Comprobar resultado y propiedades del logger, y una consulta lenta para la exclusión/parada. No afirmar éxito hasta ejecutar.
