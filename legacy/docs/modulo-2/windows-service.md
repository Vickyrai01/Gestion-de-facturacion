# Windows Service tradicional

Los comandos de desarrollo se ejecutan desde `legacy/`. La solución es `legacy/SistemaFacturacion.Legacy.slnx`; los nombres de proyecto y del servicio instalado se conservan.

Un Windows Service es un proceso administrado por el Service Control Manager (SCM), con una identidad y un ciclo de vida propios. Puede funcionar sin una sesión de usuario. `ServiceBase` permite responder a inicio y detención. Ver [introducción a servicios Windows de .NET Framework](https://learn.microsoft.com/en-us/dotnet/framework/windows-services/introduction-to-windows-service-applications).

## Código y ciclo de vida

[`FacturacionService.cs`](../../FacturacionLegacy.WindowsService/FacturacionService.cs) define `FacturacionLegacyResumen`. `OnStart` crea el procesamiento y programa un `System.Threading.Timer`, sin ejecutar una consulta bloqueante en el hilo de inicio. `OnStop` solicita tiempo adicional y dispone el procesamiento.

[`ResumenPendientes.cs`](../../FacturacionLegacy.WindowsService/ResumenPendientes.cs) contiene el núcleo compartido. `EjecutarUnaVez` crea/libera un contexto, llama a `FacturacionOperaciones.ContarPendientes` y registra fecha/hora y cantidad. No modifica estados ni registra clientes o credenciales. Los errores se registran por tipo, sin mensajes SQL sensibles. `Interlocked` evita callbacks solapados; el contexto no se comparte entre hilos. `Dispose` cancela el timer y espera los callbacks en curso. La consulta tiene timeout de comando de 30 segundos.

[`Program.cs`](../../FacturacionLegacy.WindowsService/Program.cs) selecciona SCM, ejecución única o consola. Ambos modos manuales usan el mismo núcleo, siguiendo el [enfoque de depuración documentado por Microsoft](https://learn.microsoft.com/en-us/dotnet/framework/windows-services/how-to-debug-windows-service-applications).

## Desarrollo, logs y detención

Compilar la solución o el `.csproj` con MSBuild de Visual Studio. Preparar la base antes de ejecutar:

```powershell
.\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe --una-vez
.\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe --consola
.\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe --consola --segundos 10
```

`--una-vez` devuelve 0 si consulta/registra correctamente y 1 si falla. `--consola` se detiene con Ctrl+C; `--segundos` permite un límite de 1 a 3600 segundos y usa la misma liberación normal. `IntervaloSegundos` del `.exe.config` debe estar entre 5 y 86400; su valor inicial es 60. `LogDirectory=logs` se resuelve relativo al ejecutable; cada día tiene un archivo `yyyyMMdd.log` con timestamps y cantidades. Cambiar a un directorio absoluto con permisos de escritura al instalarlo.

## Instalación opcional con SCM

LocalDB pertenece al usuario de desarrollo y normalmente no sirve para una cuenta de servicio diferente. Para instalación real local, configurar SQL Server Express, la misma base y permisos de lectura para una identidad de servicio. No ejecutar el resumen con privilegios administrativos solo para conseguir acceso a datos. Configurar también permisos de escritura en su directorio de logs.

Desde una consola administrativa, con rutas y cuenta de servicio apropiadas:

```powershell
sc.exe create FacturacionLegacyResumen binPath= '"C:\FacturacionLegacy\FacturacionLegacy.WindowsService.exe"' start= demand obj= 'NT AUTHORITY\LocalService'
sc.exe start FacturacionLegacyResumen
sc.exe query FacturacionLegacyResumen
sc.exe stop FacturacionLegacyResumen
```

Para ese ejemplo, LocalService necesita autorización SQL y permisos sobre logs. Ajustar la cadena de conexión de `FacturacionLegacy.WindowsService.exe.config`; no incrustar credenciales reales. No se instala automáticamente desde este repositorio. Si se retira una instalación de prueba, primero detener y después usar `sc.exe delete FacturacionLegacyResumen`.

## Comparación conceptual con Worker Services

El Worker Service de .NET moderno usa Generic Host y suele implementar `BackgroundService`. El host coordina arranque, parada y un CancellationToken; sus configuraciones y logging tienen otras APIs. Un Worker puede también alojarse como servicio Windows, pero no utiliza este `ServiceBase` del mismo modo. Ver [Worker Services](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers).

En el proyecto paralelo se migrará el ciclo de vida de la tarea, la consulta, configuración y logs. Debe conservarse la ausencia de cambios automáticos de estado, evitar solapamiento y comprobar la cancelación. Esta rama no incorpora Worker Services ni BackgroundService.
