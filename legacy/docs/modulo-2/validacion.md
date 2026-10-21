# Validación del Módulo 2

Entorno: Windows, Visual Studio Community 2026/MSBuild, Developer Pack .NET Framework 4.8, IIS Express y SQL Server LocalDB. Fecha de la validación: 10 de octubre de 2026, zona America/Buenos_Aires.

## Verificaciones realizadas

La solución compiló en Debug después de cada etapa implementada y en Release al finalizar, incluyendo las vistas Razor con `MvcBuildViews=true`; ambas configuraciones terminaron con código 0. Las migraciones Inicial y Usuarios se aplicaron correctamente a `FacturacionLegacyModulo2`. EF6 y BCrypt.Net-Next con sus dependencias se descargaron desde NuGet; no se actualizaron los paquetes anteriores. Se ejecutó `tools/RestaurarPaquetes.ps1` sobre los cinco `packages.config` con acceso a nuget.org: terminó con código 0. La comprobación usó paquetes ya descargados; no simuló una restauración completa en un equipo vacío.

La ejecución completa final de `tools/ValidarModulo2.ps1` pasó 63 verificaciones, usando una base temporal independiente, cuentas con contraseñas aleatorias en memoria y dos instancias propias de IIS Express. Se comprobó:

- Esquema desde migraciones, cinco clientes/ocho productos/tres facturas iniciales e inicialización idempotente.
- Hashes bcrypt con factor 12, login válido/inválido, cookies Secure/HttpOnly/SameSite=Lax, protección de rutas y logout POST.
- CRUD de clientes, validaciones y desactivación PATCH conservada del Módulo 1.
- CRUD de productos, normalización/duplicados, importes y stock no negativos.
- Facturas con varios detalles; cálculo 551.00 + 115.71 = 666.71; totales y precios enviados ignorados.
- Rechazo de cliente/producto inactivo, producto inexistente, cantidades no positivas y ausencia de detalles; sin guardar una factura parcial.
- Precios históricos independientes del precio actual, stock informativo y filtros por cliente/estado.
- Permisos directos de ADMIN/OPERADOR, emisión, anulación y conservación de detalles; inexistencia de edición de emitidas.
- WSDL y las tres operaciones SOAP, con fault controlado para estado inválido.
- Procesamiento manual de pendientes, logs de fecha/cantidad y ausencia de cambios de estado.
- Persistencia de clientes y facturas después de reiniciar la instancia MVC de prueba.
- Cuatro altas concurrentes de facturas con numeración única y dos altas simultáneas de un mismo código de producto con un único registro guardado.
- Timer con intervalo de cinco segundos, dos ciclos registrados y cierre normal del modo consola acotado.

La prueba valida el certificado IIS Express por su huella solo en el cliente local de pruebas. Usa HTTPS 44374, distinto del puerto 44372 habitual del MVC, y HTTP 60122 para WCF. Cierra los procesos que inició y elimina únicamente la base con el nombre aleatorio que creó.

## Repetir y ampliar

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1
```

Este comando parte de la raíz del repositorio; el script establece internamente `legacy/` como directorio de trabajo y compila `SistemaFacturacion.Legacy.slnx`.

El script requiere permisos para LocalDB e iniciar IIS Express, certificado de desarrollo y puertos libres. La compilación puede omitirse con `-OmitirCompilacion` solo después de haber compilado la versión actual. La pasada final incluyó las comprobaciones de concurrencia y de ejecución periódica/detención.

## Limitaciones de la evidencia

No se realizó revisión visual ni ejecución de JavaScript en un navegador. Las operaciones MVC se probaron por HTTP y SQL; el agregado/quita de filas por jQuery queda para revisión visual. No se instaló el servicio en SCM ni se probó SQL Server Express bajo otra identidad. Tampoco se probó un cliente WCF generado, tráfico remoto, un despliegue de producción, carga prolongada ni escenarios distribuidos de login.

La documentación explica esas configuraciones sin afirmar que se ejecutaron. No se construyó ni probó ninguna alternativa .NET 10. Los impuestos son didácticos, stock no se descuenta y no hay edición de facturas; las funciones de producción que exceden el módulo permanecen fuera de alcance.

## Reorganización del repositorio — 21 de octubre de 2026

Se reubicó el sistema completo en `legacy/` y la solución existente pasó a `legacy/SistemaFacturacion.Legacy.slnx`. Los paquetes se centralizaron en `legacy/packages/`, con referencias, imports de CodeDom, configuración NuGet, scripts y documentación ajustados. `modern/` contiene únicamente su README explicativo.

Una comparación SHA-256 contra el estado anterior confirmó que los 476 archivos originalmente versionados están conservados: 456 mantienen contenido idéntico y 20 tienen ajustes de rutas, proyectos, scripts o documentación. No se modificó código funcional, vistas, recursos ni metadatos de migraciones. Los 318 archivos de paquetes previamente versionados conservan su contenido y quedan incluidos por las reglas de Git en su nueva ubicación. No se eliminaron las preferencias locales ni carpetas adicionales de la aplicación.

Desde la nueva estructura se verificaron:

- Las cinco referencias de la solución, ProjectReference y HintPath; todos resuelven correctamente y los cinco proyectos conservan .NET Framework 4.8.
- Recompilación Debug y compilación Release, ambas con `MvcBuildViews=true` y código de salida 0.
- Restauración de los cinco packages.config con `legacy/tools/RestaurarPaquetes.ps1`, código de salida 0, utilizando los paquetes presentes en la nueva carpeta compartida.
- Las 63 verificaciones de `legacy/tools/ValidarModulo2.ps1`, incluido el cierre de procesos y eliminación de la base temporal propia. No se modificó la base principal.
- Enlaces actuales del README general, README de modern y documentación del Módulo 2, además de la sintaxis de los scripts PowerShell.

La primera ejecución dentro del sandbox no pudo acceder a LocalDB; se repitió con los permisos necesarios y la suite completa pasó. Esta comprobación no agrega una inspección visual ni una instalación en SCM a la evidencia anterior. La reorganización se realizó en `modulo-2-migracion-tecnica`, sin commits, pushes ni cambios en las ramas históricas.
