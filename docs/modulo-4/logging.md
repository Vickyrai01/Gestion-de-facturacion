# Logging estructurado

## Antes y después

El Módulo 2 implementaba en `ResumenPendientes.Escribir` un append de texto con timestamp y concatenación de la cantidad; también imprimía en consola interactiva. Los errores se reducían al nombre del tipo para no exponer información sensible. El nuevo `legacy/FacturacionLegacy.WindowsService/RegistroProcesamiento.cs` crea un logger Serilog local, sin variable global ni configuración del sitio MVC.

Se agregan únicamente al proyecto WindowsService:

| Paquete fijado | Asset referenciado | Compatibilidad |
| --- | --- | --- |
| Serilog 2.12.0 | `lib/net45/Serilog.dll` | .NET Framework 4.8 puede consumir net45 |
| Serilog.Sinks.File 5.0.0 | `lib/net45/Serilog.Sinks.File.dll` | .NET Framework 4.8 puede consumir net45 |

Estas versiones se eligieron para el runtime legacy, sin actualizar paquetes compartidos. Ver metadatos oficiales de [Serilog 2.12.0](https://www.nuget.org/packages/Serilog/2.12.0) y [File 5.0.0](https://www.nuget.org/packages/Serilog.Sinks.File/5.0.0). El script existente `legacy/tools/RestaurarPaquetes.ps1` restaura los cinco packages.config, incluido el actualizado. Esta selección comprueba compatibilidad declarada; no representa una auditoría de mantenimiento o seguridad de versiones.

`LogDirectory` continúa viniendo de App.config; una ruta relativa se resuelve junto al ejecutable. Se crea el directorio antes de configurar el sink. Hay dos archivos rotativos diarios, con retención de siete por sink:

- `pendientes-AAAAMMDD.json`: un evento JSON por línea, con Timestamp, Level, MessageTemplate y Properties. Se almacena la plantilla y las propiedades, no solo un mensaje renderizado.
- `resumen-AAAAMMDD.log`: línea de cantidad y fecha para conservar el contrato de lectura de `legacy/tools/ValidarModulo2.ps1`. El nombre difiere del antiguo `AAAAMMDD.log`; las comprobaciones usan `*.log`. Los errores completos del recorrido están en JSON, no en este resumen.

Los eventos estructurados se observan en archivos. Se conserva además la cantidad y el tipo de error en consola interactiva mediante Console; el modo periódico informa inicio y detención. No se agregó un sink de consola adicional.

```csharp
log.Information("Inicio de procesamiento de facturas pendientes");
log.Information("Se encontraron {CantidadFacturas} facturas pendientes", cantidad);
log.Information("Procesamiento finalizado en {DuracionMs} ms", inicio.ElapsedMilliseconds);
// Si falla: evento Error con TipoError y retorno false.
```

`CantidadFacturas` y `DuracionMs` son propiedades numéricas consultables. No usar interpolación ni concatenación para sustituir la plantilla. No se registran facturas, nombres, documentos, credenciales, conexiones, mensajes de excepción ni stack traces. La rama de error conserva solo `error.GetType().Name`.

## Demostración local

Desde la raíz, luego de restaurar/compilar y configurar SQL:

```powershell
$exe = '.\legacy\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe'
& $exe --una-vez
Get-Content .\legacy\FacturacionLegacy.WindowsService\bin\Debug\logs\pendientes-*.json |
    ForEach-Object { $_ | ConvertFrom-Json } |
    Select-Object Timestamp, Level, MessageTemplate, Properties
```

Esperar inicio, cantidad y finalización si SQL responde. En una configuración de prueba con conexión inválida, esperar inicio y Error con TipoError; salida 1, sin finalización exitosa. Restaurar después la configuración. No registrar la cadena inválida en la evidencia.

El constructor verifica que el directorio puede crearse, pero Serilog puede manejar internamente fallos posteriores del sink: una salida 0 del proceso acredita la consulta y la emisión del evento a la API, no garantiza entrega durable. Revisar que el archivo exista y contenga eventos. No hay cola durable ni reintentos de escritura. El constructor por defecto posee el logger; `Dispose` lo cierra después de esperar el trabajo activo. Un logger inyectado es propiedad del llamador.

## Comparación con .NET 10

En el proyecto moderno, la aplicación recibiría `Microsoft.Extensions.Logging.ILogger<ResumenPendientes>` y usaría `LogInformation("Se encontraron {CantidadFacturas} facturas pendientes", cantidad)`. El host aporta categorías, niveles, scopes y configuración de proveedores. Serilog puede actuar como proveedor, pero su `Serilog.ILogger` no es la misma interfaz. La plantilla y propiedades sobreviven a la migración; el registro/composición y la API cambian. No se instala Microsoft.Extensions.Logging ni un host moderno en este proyecto.
