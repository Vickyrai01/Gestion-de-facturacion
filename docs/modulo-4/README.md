# Módulo 4 — Modernización de código .NET

Base común: `modulo-2-migracion-tecnica` (`cad49ff`). La rama `modulo-4-modernizacion` incluye el Módulo 3 (`0772704`) y el Módulo 4 (`d0822e6`) mediante el merge normal `71d866e`. La integración funcional reutiliza los contratos y adaptadores compartidos. Se mantiene .NET Framework 4.8, MVC 5 y EF6; no se modifica `modern/` ni se realiza push.

| Tema | Demostración ejecutable ahora | Integración posterior |
| --- | --- | --- |
| Async/await | EF6: listado acotado y detalle de productos por consola | Contratos async ya integrados en Productos; recorrido HTTP futuro |
| Logging | Serilog con propiedades y archivos JSON | `ILogger<T>` y configuración del host .NET 10 |
| Background jobs | Timer, exclusión mutua y parada del Windows Service | `BackgroundService` / `IHostedService` |
| DI | Interfaces compartidas y operación inyectada en el resumen | Contenedor nativo en ASP.NET Core |
| Minimal APIs | Auditoría y ejemplos solo en Markdown | Proyecto .NET 10 independiente |
| Clean Architecture | Propuesta de límites y responsabilidades | Contratos compartidos del Módulo 3 integrados; separación adicional futura |

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

Las lecturas async viven ahora en `legacy/FacturacionLegacy.Datos/Productos/ConsultaProductosEf6.cs`, usan `IConsultaProductos` y devuelven `ProductoConsultaDto`. Se eliminaron `ConsultasProductosAsync.cs`, su DTO local y `ConsultaPendientes.cs` del Windows Service, después de revisar sus consumidores. El conteo está en `IRepositorioFacturas` / `RepositorioFacturasEf6`; la fachada `FacturacionOperaciones.ContarPendientes` conserva su firma y delega allí.

`legacy/FacturacionLegacy.WindowsService/ComposicionProcesos.cs` crea el contexto por ciclo; `ResumenPendientes` recibe la operación mediante `Func<int>` y el logger. `Program` compone la consulta de productos con un contexto que dura hasta terminar el await. Se mantiene el patrón de composición manual de `FacturacionWeb`, sin contenedores adicionales. No se modifican controllers, vistas, autenticación, WCF ni flujos de escritura en esta integración.

Serilog 2.12.0 y Serilog.Sinks.File 5.0.0 siguen siendo las únicas dependencias del Módulo 4; esta integración no agrega ni actualiza paquetes. El arnés existente de arquitectura incorpora `legacy/tools/pruebas-modulo-3/PruebasIntegracionModulo4.cs` para comprobar consultas con proveedor EF6 en memoria y el resumen con logger en memoria.

## Validación y evidencia

Revisión realizada el 21/10/2026 en macOS: estado inicial limpio, base y rama de trabajo verificadas; inspección del código síncrono, timer, permisos MVC, contratos WCF y script de validación anterior. Se verificaron XML, inclusiones de proyectos, rutas de documentación y diff. Se confirmó que las ramas no modificaron archivos comunes desde la base y que Git considera el Módulo 3 ya integrado. No hubo conflictos textuales; los ajustes son de contratos y composición. No hay `dotnet`, `mono`, `msbuild`, `nuget` ni `csc` en este entorno. No se ejecutaron compilación, restauración, SQL Server, MVC, consultas async, escritura de logs ni timer. La evidencia del Módulo 2 corresponde a aquella rama y no valida estos cambios.

Pendiente en Windows:

1. Restaurar y compilar Debug y Release, incluyendo Razor (`MvcBuildViews=true`).
2. Comparar los primeros 20 Id/Codigo/PrecioUnitario con SQL ordenado por Codigo, Id. Probar detalle existente, inexistente, id inválido y Ctrl+C. Verificar salidas 0/1/2 según caso. Una consulta rápida puede terminar antes de cancelar; probar también un token ya cancelado invocando el método directamente.
3. Ejecutar `--una-vez`; leer JSON con `Get-Content ... | ConvertFrom-Json`: inicio, cantidad numérica y finalización con duración. Comparar cantidad con SQL. Probar conexión inválida en una base de pruebas: salida 1 y `TipoError`, sin cadena de conexión ni mensaje SQL.
4. Ejecutar consola con intervalo 5 y duración 12, luego Ctrl+C en modo sin límite. Confirmar varios ciclos, ausencia de solapamiento y que no aparecen eventos nuevos tras salir. Comparar estados/cantidades de facturas antes/después.
5. Ejecutar `legacy/tools/ValidarModulo2.ps1` para regresión MVC/WCF/facturación; se conserva la línea `Facturas pendientes:` en un `.log` para sus comprobaciones. Revisar visualmente MVC e instalar opcionalmente el servicio con la cuenta correcta según la guía del Módulo 2.
6. Ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1`. Además de las pruebas de arquitectura, compila y ejecuta las nuevas verificaciones de integración sin SQL: orden/límite de listado async, inactivos, detalle ausente, id inválido, token cancelado, conservación de consultas síncronas, logging numérico, parada y error sin mensaje sensible. El proveedor en memoria no valida traducción SQL ni cancelación en vuelo; realizar también los pasos anteriores con SQL real. Probar una consulta lenta para exclusión/parada. Este arnés fue actualizado, pero no se ejecutó aquí.
