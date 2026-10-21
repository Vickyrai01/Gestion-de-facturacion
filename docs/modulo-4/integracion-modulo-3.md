# Integración posterior con el Módulo 3

No se leyó, fusionó ni asumió el estado de la rama de arquitectura. Esta auditoría describe la base Módulo 2 y propone decisiones para acordar cuando el Módulo 3 esté disponible. Las interfaces locales del Módulo 4 son didácticas; no son contratos definitivos del sistema.

## Auditoría conceptual de Clean Architecture

| Componente real | Acoplamiento/responsabilidad actual | Propuesta de integración |
| --- | --- | --- |
| `legacy/legacyGestionDeFacturacion.web/Controllers/ProductosController.cs` | Crea DbContext; consulta, valida, guarda y entrega vistas | Delegar caso de uso en servicio del Módulo 3; inyectarlo; mantener permisos y comportamiento |
| `legacy/legacyGestionDeFacturacion.web/Controllers/FacturasController.cs` | Acceso EF, creación, filtros, estados y presentación | Controller como adaptador; casos de uso separados; evitar cambiar transacción y numeración al introducir async |
| `legacy/FacturacionLegacy.Datos/FacturacionOperaciones.cs` | Métodos estáticos mezclan reglas, transacciones, SQL y consultas | Separar reglas de aplicación y adaptador de persistencia según el diseño del Módulo 3 |
| `legacy/FacturacionLegacy.Datos/FacturacionDbContext.cs` | Infraestructura EF6 y selección de conexión | Mantener en infraestructura; ciclo de vida por solicitud/operación, nunca singleton ni compartido entre hilos |
| `legacy/FacturacionLegacy.Wcf/FacturaConsultaService.svc.cs` | Adaptación SOAP, faults y proyección con dependencia directa de datos | Adaptador de entrada que invoca casos de consulta; conservar contrato SOAP hasta migración explícita |
| `legacy/FacturacionLegacy.WindowsService/ConsultaPendientes.cs` | Interfaz local y adaptador EF síncrono | Reutilizar contrato del Módulo 3, agregar async con token si corresponde; eliminar duplicación de abstracciones |
| `legacy/FacturacionLegacy.WindowsService/ResumenPendientes.cs` | Agenda, exclusión, parada y logging; consulta delegada | Mantener coordinación en host y caso de uso fuera del timer; logger en borde/adaptador |
| `legacy/FacturacionLegacy.WindowsService/ConsultasProductosAsync.cs` | Demo EF6 de lectura/DTO autocontenida | Absorber consulta en servicio/repositorio acordado; no hacer depender la web del ejecutable del servicio |
| `legacy/FacturacionLegacy.WindowsService/RegistroProcesamiento.cs` | Configuración concreta Serilog | Composición en entrada legacy; proveedor y configuración del host en .NET 10 |

El dominio debe expresar reglas sin depender de MVC, WCF, Serilog, EF o del timer. Aplicación coordina casos de uso y define puertos. Infraestructura implementa persistencia; MVC/WCF/servicio son adaptadores de entrada. La dirección de dependencias apunta hacia dominio/aplicación. Una interfaz por sí sola no crea Clean Architecture: la demo mantiene su interfaz local por aislamiento y no intenta convertir todo el sistema en capas ahora.

## Trabajo diferido

1. Comparar ambas ramas sin merge automático; acordar nombres, propiedad de contexto, DTO, errores y autorización con la desarrolladora del Módulo 3.
2. Encajar las dos lecturas de productos y el conteo detrás de sus contratos. Propagar Task/token de extremo a extremo solo en recorridos seleccionados. Eliminar interfaces de demostración duplicadas cuando exista reemplazo verificable.
3. Incorporar DI en el composition root definido por Módulo 3. En legacy requerirá composición/resolución MVC 5 explícita; DI nativa, Minimal APIs y Generic Host corresponden al proyecto .NET 10 separado.
4. Incorporar logs de operación sin PII, con categorías/scopes en el host moderno. Preservar semántica de cantidad y error, no depender de nombres físicos de archivos para observabilidad futura.
5. Migrar el alojamiento periódico a BackgroundService con scope por ciclo y cancelación. No habilitar emisión automática; decidir política de fallos y coordinación entre instancias por separado.
6. Implementar contratos HTTP y políticas de los dos endpoints propuestos solo en el futuro proyecto. No presuponer que la vista MVC es un contrato JSON.

## Riesgos concretos y validación

Los puntos más probables de conflicto textual son el `.csproj`, `packages.config` y `ResumenPendientes.cs` del Windows Service. Los controllers y `FacturacionOperaciones` no cambiaron aquí, pero sus firmas pueden cambiar en el Módulo 3; revisar la llamada de `ConsultaPendientesEf` antes de integrar. No resolver conflictos aceptando un lado entero sin inspección.

La consulta de productos está dentro del ejecutable para evitar un nuevo proyecto/capa competidora; no usar esa ubicación como arquitectura objetivo. La parada del resumen espera SQL síncrono y la exclusión solo protege una instancia local. DI no convierte automáticamente operaciones en thread-safe. Conservar los datos, cálculos, numeración y transacciones al propagar async.

Los nuevos logs usan JSON y un archivo de compatibilidad, con retención de siete archivos por sink. Los consumidores externos que dependían de `AAAAMMDD.log` o cantidades en consola deberán adaptarse. Los paquetes seleccionados soportan Framework, pero hay que comprobar restauración y carga de DLL en Windows. Si cambia el proveedor, conservar plantillas/propiedades y reglas de privacidad.

Después de integrar: compilar Debug/Release con vistas; ejecutar regresión Módulo 2, lecturas async, cancelación, conteos y fallos sin datos sensibles; comprobar ciclos sin superposición y cierre; revisar MVC/WCF y que no cambien estados de facturas. La validación pendiente detallada está en [README](README.md). No se afirman pruebas funcionales ejecutadas en esta rama.
