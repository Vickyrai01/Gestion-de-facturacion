# Integración de los Módulos 3 y 4

Base común `cad49ff` (`modulo-2-migracion-tecnica`). El merge normal `71d866e` ya estaba en la rama al iniciar esta revisión y contiene `d0822e6` (Módulo 4) y `0772704` (Módulo 3, referencia `origin/modulo-3-arquitectura`). El árbol inicial estaba limpio. Se ejecutó fetch y `git merge --no-ff origin/modulo-3-arquitectura` confirmó `Already up to date`. No se reescribe ese merge ni se modifica la rama de arquitectura, main o modern/.

## Conflictos y ajustes

Las ramas modificaron 26 y 14 archivos respectivamente desde la base, sin rutas en común. No hubo conflictos textuales nuevos ni archivos sin resolver. El merge existente había conservado ambas implementaciones, pero faltaba integrar contratos y documentación.

| Incompatibilidad funcional | Resolución |
| --- | --- |
| DTO `ProductoConsulta` y acceso EF propios en `WindowsService/ConsultasProductosAsync.cs` | Se eliminan; `Datos/Productos/IConsultaProductos` incorpora ListarAsync/ObtenerAsync y `ConsultaProductosEf6` los implementa usando `ProductoConsultaDto`. La proyección se comparte con los métodos síncronos. |
| Contrato local IConsultaPendientes y adaptador separado | Se eliminan; el conteo se incorpora a IRepositorioFacturas/RepositorioFacturasEf6. FacturacionOperaciones conserva su firma pública delegando en el repositorio. |
| Contexto creado dentro de la demo async en lugar del adaptador del Módulo 3 | Program compone IConsultaProductos y posee el contexto hasta finalizar el await. |
| Timer ligado a la interfaz local de demostración | Recibe Func<int> y Serilog.ILogger. ComposicionProcesos suministra la operación creando un contexto por ciclo, usando el repositorio compartido. |
| Dobles del arnés con contratos anteriores | Se amplían con los miembros nuevos y se agregan verificaciones de integración al arnés existente. |
| Guías que suponían que Módulo 3 aún no estaba disponible | Se actualizan rutas, componentes y ejemplos al código integrado. |

Los archivos de código anteriores solo tenían consumidores dentro del propio Windows Service, comprobados mediante búsqueda del repositorio. No se eliminan consultas ni comportamientos: listado async incluye activos e inactivos, orden Codigo/Id, máximo 20; detalle por id devuelve null si falta. Los métodos síncronos ListarActivos/Obtener(int[]) conservan sus filtros y uso en creación. El nuevo DTO compartido tiene más campos, pero la consola sigue imprimiendo solo Id, Codigo y PrecioUnitario.

## Arquitectura resultante

`FacturasController → IServicioFacturacion / ServicioFacturacion → dominio y contratos → adaptadores EF6` conserva el refactor del Módulo 3. No se modifican su dominio, servicio, controller ni vistas. Las interfaces y capas continúan separadas lógicamente dentro de FacturacionLegacy.Datos; no se crean proyectos adicionales.

La consola utiliza IConsultaProductos, el job utiliza la operación compuesta sobre IRepositorioFacturas, y el logger queda en el anfitrión. La web conserva FacturacionWeb como composición manual por petición; el servicio usa el mismo patrón con un contexto por operación. No se agrega un contenedor ni se mantiene una segunda familia de DTO/repositorios para la demo.

Se conservan Serilog, archivos JSON y resumen de texto, exclusión entre ciclos y parada que espera el trabajo activo. La consulta periódica sigue siendo síncrona; no cancela en vuelo el SQL. La cancelación con token está en las lecturas async de productos. La exclusión es local al proceso.

## Configuración y límites preservados

Sin cambios en Web.config, App.config, paquetes, connection strings, WCF, autenticación/roles, migraciones ni reglas de negocio. Se conserva Serializable, secuencia, validación de clientes/productos, cálculos y cambios condicionales de estado. La fachada mantiene sus firmas para WCF y consumidores existentes. Todos los proyectos siguen declarando .NET Framework 4.8, MVC 5 y EF6; no se agregan ASP.NET Core, EF Core ni Minimal APIs ejecutables.

Los contratos IConsultaProductos e IRepositorioFacturas tienen miembros nuevos: cualquier implementación externa no versionada deberá incorporarlos. Se actualizaron todas las implementaciones encontradas en este repositorio, incluidos los dobles de prueba. Los adaptadores reciben el contexto sin poseerlo; respetar su ciclo de vida y no compartirlo entre consultas concurrentes.

## Validación

En macOS, revisión estática de Git, XML, proyectos, configuraciones, consumidores y documentación; `git diff --check`. No hay .NET/Mono/MSBuild/PowerShell ni SQL Server disponibles, por lo que no se ejecutaron compilación ni pruebas funcionales. No confundir resultados históricos del Módulo 2/3 con pruebas de esta integración.

El arnés existente `legacy/tools/ValidarModulo3.ps1` ahora incorpora pruebas sin SQL de los métodos async sobre un DbSet/proveedor EF6 en memoria y del resumen con logger en memoria. No reemplaza la validación del proveedor SQL real. Pendiente ejecutar en Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\RestaurarPaquetes.ps1
msbuild .\legacy\SistemaFacturacion.Legacy.slnx /p:Configuration=Debug /p:MvcBuildViews=true
msbuild .\legacy\SistemaFacturacion.Legacy.slnx /p:Configuration=Release /p:MvcBuildViews=true
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1
```

La regresión del Módulo 2 cubre CRUD, facturación, totales, autenticación/roles, WCF y servicio. Completar las demostraciones async con SQL, error/cancelación, JSON y timer descritas en [README](README.md), además de revisión visual MVC e instalación opcional del servicio. Ninguna prueba funcional se declara aprobada aquí.

Minimal APIs, DI nativa y BackgroundService siguen siendo material educativo para un proyecto .NET 10 independiente; no forman parte de esta integración legacy.
