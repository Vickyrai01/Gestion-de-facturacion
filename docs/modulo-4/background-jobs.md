# Procesamiento en segundo plano

Se reutiliza `legacy/FacturacionLegacy.WindowsService`, sin segundo servicio. `FacturacionService` hereda `ServiceBase`, usa el nombre `FacturacionLegacyResumen`, inicia `ResumenPendientes` en `OnStart` y lo dispone en `OnStop` solicitando 60 segundos adicionales al SCM.

## Mecanismo actual

`ResumenPendientes.Iniciar` valida `IntervaloSegundos` entre 5 y 86400; programa `System.Threading.Timer` con primer tick inmediato. Los callbacks se ejecutan en el ThreadPool. La consulta de conteo sigue siendo síncrona de forma intencional, para conservar el recorrido anterior como comparación; ocupa un hilo durante SQL. La demo async separada de productos no se llama desde el timer.

`EjecutarUnaVez` usa un lock breve para aceptar una ejecución. Si hay una activa, el tick se omite (retorna true como en la versión anterior); no se encolan conteos ni se comparte DbContext entre ciclos. La consulta se inyecta y el adaptador EF fija timeout de comando de 30 segundos. Se registran inicio, cantidad y duración. Los errores producen un evento Error con tipo técnico y false; el timer permanece activo para el siguiente ciclo. El modo único propaga false como salida 1. El modo periódico sale 0 cuando termina normalmente; no agrega los errores de todos sus ciclos en el código de salida: consultar el JSON.

`Dispose` marca detenido, dispone el timer y espera con `Monitor.Wait` a la operación activa. Los callbacks que ya estaban encolados retornan sin consultar ni registrar. Luego dispone únicamente el logger propio. Se rechaza iniciar dos veces o después de disponer. La exclusión incluye ejecuciones manuales, además de callbacks. El propietario debe coordinar una sola llamada de cierre; el método no es un protocolo para múltiples cierres concurrentes.

Detención cooperativa aquí significa dejar de aceptar ciclos y esperar. No se intenta abortar un hilo ni cancelar la consulta síncrona que ya está ejecutando SQL. El timeout de comando no es un límite total de apagado: conexión, proveedor y escritura de logs también pueden demorar. El timeout de conexión depende de su cadena. La cancelación con token se muestra en el recorrido async de productos.

## Ejecutar y observar

Desde la raíz, con la base preparada:

```powershell
$exe = '.\legacy\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe'
& $exe --una-vez
& $exe --consola --segundos 12
& $exe --consola
# Ctrl+C detiene el último modo y espera el trabajo activo.
```

Para ver varios ciclos cortos usar `IntervaloSegundos=5` en `.exe.config`. Comprobar propiedades `CantidadFacturas` y `DuracionMs` en `logs/pendientes-*.json`. Se ejecuta únicamente `SELECT COUNT`: no emite, anula, actualiza ni elimina facturas. Confirmar con una comparación de estados antes y después, no solo mediante el resultado del log.

No se instala automáticamente en Windows. Instalación y cuenta de servicio: `legacy/docs/modulo-2/windows-service.md`. LocalDB es por usuario; para SCM elegir SQL Server y permisos de lectura de esa identidad, además de escritura en el directorio de logs. Los requisitos del servicio original se mantienen.

## Evolución educativa, pendiente de .NET 10

`IHostedService` define StartAsync/StopAsync; `BackgroundService` proporciona el ciclo `ExecuteAsync(CancellationToken)`. El Generic Host coordina configuración, DI, logging y señales de apagado. Un bucle que aguarda cada trabajo y luego el siguiente tick puede conservar la ausencia de solapamiento. Hay que propagar el token a consulta/espera, separar cancelación esperada de error y definir qué fallos se reintentan o detienen el host.

Crear un scope por ciclo con `IServiceScopeFactory` para resolver servicios/contextos scoped, sin guardarlos en el singleton hospedado. Mantener lógica de consulta fuera de la clase de alojamiento. En .NET 10 este ciclo puede alojarse también como servicio Windows, pero no se agrega ese código aquí. Si en el futuro existen efectos sobre facturas, harán falta idempotencia, transacciones y coordinación entre instancias; el lock actual solo coordina un objeto en un proceso. Ver [servicios scoped en BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service).
