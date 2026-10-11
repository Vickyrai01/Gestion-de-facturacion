# Módulo 2 — Migración técnica del sistema legacy

La rama `modulo-2-migracion-tecnica` parte del commit del Módulo 1. Incorpora persistencia SQL Server/EF6, productos, facturas, consultas WCF SOAP, un Windows Service y Forms Authentication. Todos los proyectos siguen apuntando a .NET Framework 4.8; MVC sigue en 5.2.9. La migración a .NET 10 se realizará en un proyecto independiente.

## Solución y dependencias

Abrir `legacyGestionDeFacturacion.web/legacyGestionDeFacturacion.web.slnx` en Visual Studio para Windows con soporte para `.slnx`. La solución contiene:

| Proyecto | Responsabilidad |
| --- | --- |
| `legacyGestionDeFacturacion.web` | MVC 5: inicio, clientes, productos, facturas, reportes y login. |
| `FacturacionLegacy.Datos` | Entidades, DbContext, migraciones, operaciones de facturas y hashing de contraseñas. No referencia al proyecto web. |
| `FacturacionLegacy.Wcf` | Servicio SOAP `FacturaConsultaService.svc`, contratos y DTOs. |
| `FacturacionLegacy.WindowsService` | `ServiceBase`, timer y consulta periódica de pendientes; modos de consola y ejecución única. |
| `FacturacionLegacy.Tools` | Ejecutable para aplicar/generar migraciones y crear cuentas locales sin contraseñas en el código. |

Se mantiene `packages.config`. Nuevos paquetes únicos: EntityFramework 6.5.1; BCrypt.Net-Next 4.0.3 y sus dependencias System.Memory 4.5.4, System.Buffers 4.5.1, System.Numerics.Vectors 4.5.0 y System.Runtime.CompilerServices.Unsafe 4.5.3. Se usan sus assets compatibles con .NET Framework; las versiones anteriores de MVC, Razor y frontend se conservan. `NuGet.Config` dirige la restauración a `legacyGestionDeFacturacion.web/packages`. Las bibliotecas nuevas descargadas y `tools/nuget.exe` no se versionan.

## Preparación local

Requisitos: Windows, Visual Studio con Desarrollo de ASP.NET y web, MSBuild/targets web, Developer Pack 4.8, IIS Express con certificado de desarrollo y SQL Server LocalDB. Para WCF debe estar disponible la activación HTTP de WCF/.NET Framework en el equipo. SQL Server Express es una alternativa si no hay LocalDB.

Desde la raíz, en Developer PowerShell de Visual Studio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\RestaurarPaquetes.ps1
MSBuild .\legacyGestionDeFacturacion.web\legacyGestionDeFacturacion.web.slnx /t:Build /p:Configuration=Debug /p:MvcBuildViews=true
.\FacturacionLegacy.Tools\bin\Debug\FacturacionLegacy.Tools.exe migrar
.\FacturacionLegacy.Tools\bin\Debug\FacturacionLegacy.Tools.exe usuarios-demo
```

La restauración requiere Internet y puede descargar NuGet CLI oficial si falta. No actualiza paquetes. También se puede restaurar desde Visual Studio.

`migrar` crea/actualiza `FacturacionLegacyModulo2` y prepara cinco clientes, ocho productos y tres facturas con detalles, una por estado. Repetirlo no duplica ejemplos ni sobrescribe ediciones. La web no ejecuta migraciones automáticamente. `usuarios-demo` pide contraseñas ocultas para `admin` y `operador`: al menos 12 caracteres, hasta 72 bytes UTF-8. Guarda solo hashes bcrypt con salt y factor 12. No hay contraseña predeterminada ni registro público; las cuentas existentes no se sobrescriben. Opcionalmente acepta las variables de proceso `FACTURACION_ADMIN_PASSWORD` y `FACTURACION_OPERADOR_PASSWORD` para automatización local; eliminarlas al terminar y no escribirlas en archivos versionados.

La conexión habitual está en `Web.config` del MVC y WCF y en `App.config` de Tools y Windows Service:

```xml
<add name="FacturacionDb"
     connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=FacturacionLegacyModulo2;Integrated Security=True;MultipleActiveResultSets=True"
     providerName="System.Data.SqlClient" />
```

Para Express, reemplazar el origen por `.\SQLEXPRESS` en esos cuatro archivos y conceder permisos a la identidad Windows que ejecuta cada proceso. Mantener la misma base para todos. La variable opcional `FACTURACION_DB_CONNECTION` permite aislar las pruebas sin cambiar esos archivos; no se necesita en la ejecución habitual.

## MVC, WCF y proceso de fondo

Establecer MVC como proyecto de inicio y ejecutar con IIS Express/F5. Usar `https://localhost:44372/` y confiar en su certificado de desarrollo. HTTPS es obligatorio para MVC y sus cookies; HTTP no es una alternativa para iniciar sesión. Acceder con las contraseñas elegidas para las cuentas locales.

Desde la raíz, iniciar WCF en otra consola después de compilar:

```powershell
& "$env:ProgramFiles\IIS Express\iisexpress.exe" /path:"$PWD\FacturacionLegacy.Wcf" /port:60122 /systray:false
```

Consultar `http://localhost:60122/FacturaConsultaService.svc?wsdl`. Se utiliza `basicHttpBinding` SOAP 1.1 y se aceptan operaciones solo desde loopback para esta demostración. No comparte la cookie Forms del MVC. Detener con Ctrl+C. [wcf.md](wcf.md) explica consumo y contrato.

El procesamiento manual usa la misma base y no cambia estados:

```powershell
.\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe --una-vez
.\FacturacionLegacy.WindowsService\bin\Debug\FacturacionLegacy.WindowsService.exe --consola
```

En consola, Ctrl+C detiene el timer y espera la liberación de recursos. También se puede usar `--consola --segundos 10` para una demostración acotada. Los logs están en `bin/Debug/logs` por defecto; intervalo y directorio se configuran en el `.exe.config`. La instalación con SCM y SQL Server Express se describe en [windows-service.md](windows-service.md); no es necesaria para desarrollar con LocalDB.

## Reglas, permisos y límites

Clientes conservan rutas y vistas del Módulo 1, incluida la desactivación POST/PATCH. Productos permiten listar, consultar, crear, editar y desactivar; código obligatorio/único, nombre obligatorio, precio y stock no negativos. Códigos se recortan y normalizan a mayúsculas. La cultura de importes se fija a en-US y la UI está en español: introducir punto decimal. Los precios se redondean a dos decimales.

Facturas permiten múltiples detalles, filtros por cliente/estado y consulta de importes. Se validan clientes/productos activos y cantidades positivas, se toman precios de la base y se calcula `Subtotal + Impuestos = Total`. La tasa fija `TasaImpuestos=0.21` en `Web.config` es solo didáctica. Las fechas se guardan/muestran en UTC. El número lo asigna una secuencia SQL Server; puede tener huecos. Se guardan precios históricos; el stock es informativo. Una transacción guarda cabecera y detalles. No se implementa edición de facturas de ningún estado; solo creación, emisión de pendientes y anulación conservando registros. La factura tampoco es un comprobante fiscal real.

ADMIN gestiona clientes/productos, crea/consulta facturas, emite/anula y consulta reportes. OPERADOR consulta clientes/productos y crea/consulta facturas. Las restricciones se aplican en el servidor y los botones acompañan los permisos. No hay administración pública de usuarios, MFA, recuperación de contraseña ni auditoría de cambios; ver [autenticacion.md](autenticacion.md).

## Material y pruebas

[entity-framework.md](entity-framework.md), [wcf.md](wcf.md), [windows-service.md](windows-service.md), [autenticacion.md](autenticacion.md), [guia-migracion.md](guia-migracion.md) y [ejercicios.md](ejercicios.md) cubren las clases 3, 4 y 5 con archivos y métodos reales. No contienen implementaciones modernas.

`tools/ValidarModulo2.ps1` compila y ejecuta verificaciones contra una base temporal con nombre aleatorio. Utiliza HTTPS 44374 para MVC y HTTP 60122 para WCF; ambos puertos deben estar libres. El cliente de prueba valida el certificado local por su huella, sin alterar la confianza de Windows. Genera contraseñas aleatorias en memoria y cierra sus procesos y elimina su propia base en `finally`. Requiere acceso a LocalDB e iniciar IIS Express. Los resultados y pendientes se registran en [validacion.md](validacion.md).
