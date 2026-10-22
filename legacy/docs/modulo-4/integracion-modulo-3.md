# Arquitectura aplicada a la modernización

Los contratos y la separación de responsabilidades del Módulo 3 permiten incorporar async/await, logging y procesamiento periódico sin mezclar reglas de negocio con infraestructura o presentación.

## Componentes y responsabilidades

| Componente | Responsabilidad | Concepto para demostrar |
| --- | --- | --- |
| `IConsultaProductos` / `ConsultaProductosEf6` | Consultas síncronas y asíncronas con un mismo `ProductoConsultaDto` | Async cambia la espera de I/O, no el contrato de datos ni las reglas del catálogo. |
| `IRepositorioFacturas` / `RepositorioFacturasEf6` | Persistencia y conteo de facturas pendientes | El acceso SQL queda fuera del timer y del dominio. |
| `Program.MostrarProductosAsync` | Composición de la consulta y presentación en consola | El contexto permanece vivo hasta finalizar el await. |
| `ComposicionProcesos` | Un contexto por ciclo de procesamiento | Las dependencias se crean en el anfitrión y no se comparten entre ejecuciones. |
| `ResumenPendientes` | Periodicidad, exclusión, parada y registro de resultados | Inyección de la operación mediante `Func<int>` y del logger mediante `Serilog.ILogger`. |
| `FacturacionOperaciones` | Fachada de compatibilidad | Los consumidores existentes pueden conservar sus firmas mientras delegan en componentes especializados. |

Las rutas de estos componentes parten de `legacy/FacturacionLegacy.Datos/` y `legacy/FacturacionLegacy.WindowsService/`, según su anfitrión.

## Recorridos para la clase

La consulta de productos sigue el recorrido `Program → IConsultaProductos → ConsultaProductosEf6 → SQL Server`. `ListarAsync` incluye activos e inactivos, ordena por Codigo/Id y limita el resultado a 20. `ObtenerAsync` devuelve null si el id no existe. Los métodos síncronos `ListarActivos` y `Obtener(int[])` sirven al caso de creación de facturas y conservan sus propios filtros.

El procesamiento periódico sigue `ResumenPendientes → operación inyectada → ComposicionProcesos → IRepositorioFacturas → RepositorioFacturasEf6`. Cada ciclo posee un contexto; el logger pertenece al anfitrión. El conteo es síncrono y la parada espera que termine la operación activa. La cancelación con token se demuestra en las consultas async de productos.

El flujo de negocio sigue `FacturasController → IServicioFacturacion / ServicioFacturacion → dominio y contratos → adaptadores EF6`. Las reglas de cantidades, redondeo, impuestos y totales pertenecen al dominio; la transacción Serializable, la secuencia y el guardado corresponden a persistencia. El logging y el timer no deciden estados de facturas.

## Límites de la separación

Las capas son lógicas dentro de FacturacionLegacy.Datos. La web utiliza `FacturacionWeb` para la composición manual por petición y el servicio crea dependencias por operación. Los adaptadores reciben el contexto sin poseerlo: no deben disponerlo ni compartirlo entre consultas concurrentes.

Las interfaces facilitan sustituir dependencias en pruebas, pero no garantizan seguridad entre hilos, compatibilidad de runtime ni traducción SQL. La exclusión del job protege una instancia dentro de un proceso; no coordina varios servicios distribuidos.

## Ejercicio de validación

Desde la raíz del repositorio, en Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\RestaurarPaquetes.ps1
msbuild .\legacy\SistemaFacturacion.Legacy.slnx /p:Configuration=Debug /p:MvcBuildViews=true
msbuild .\legacy\SistemaFacturacion.Legacy.slnx /p:Configuration=Release /p:MvcBuildViews=true
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1
```

El primer validador ejercita dominio, coordinación y consultas/logging con dependencias en memoria. El segundo utiliza LocalDB e IIS Express para CRUD, facturación, totales, autenticación/roles, WCF y servicio. Completar las demostraciones async con SQL, cancelación, JSON y timer descritas en [README](README.md). Los dobles en memoria no validan traducción SQL ni cancelación en vuelo.

## Comparación con .NET 10

El contenedor nativo puede sustituir la composición manual; `ILogger<T>` permite separar la aplicación del proveedor de logs; `BackgroundService` coordina el ciclo de vida del job y Minimal APIs adapta casos de consulta a HTTP. Estos conceptos se estudian para el proyecto .NET 10 independiente. La aplicación del curso continúa utilizando .NET Framework 4.8, MVC 5 y EF6.
