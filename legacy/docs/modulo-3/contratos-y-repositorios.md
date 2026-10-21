# Contratos, DTOs y persistencia

## Límites útiles en este sistema

[`Facturacion/Contratos.cs`](../../FacturacionLegacy.Datos/Facturacion/Contratos.cs) contiene el contrato del caso de uso. `CrearFacturaRequest` agrupa ClienteId y las líneas existentes. No agrega Id, Estado, precio ni totales editables. `LineaFactura` continúa en `FacturacionOperaciones.cs` para conservar compatibilidad con el formulario y otras llamadas del Módulo 2: no se duplicó otro DTO con las mismas dos propiedades.

`FacturaResumenDto` lleva los campos del listado y los nombres del cliente. `FacturaDetalleDto` agrega documento y líneas históricas. `FacturaLineaDto` contiene código/nombre de producto e importes necesarios para la vista. `ReporteEstadoDto` reemplaza la clase ReporteEstado definida antes dentro del archivo del controller. Estos tipos resuelven un acoplamiento real: las vistas ya no recorren asociaciones EF ni dependen de que un contexto siga disponible.

Los DTOs no son entidades persistidas y no se agregan a DbContext. Tampoco reemplazan los DataContract públicos SOAP: WCF conserva sus contratos anteriores.

## Puertos acotados

`IRepositorioFacturas` ofrece operaciones de facturación: transacción de creación, Guardar, Consultar, Obtener, Existe, Emitir, Anular y ResumirPorEstado. No reproduce DbSet entero ni ofrece un repositorio genérico por tabla. La interfaz no devuelve IQueryable ni objetos EF. Su implementación, [`RepositorioFacturasEf6`](../../FacturacionLegacy.Datos/Facturacion/Infraestructura/RepositorioFacturasEf6.cs), materializa DTOs y concentra el SQL y los mapeos.

Las consultas de Clientes y Productos tienen contratos pequeños en `Clientes/Contratos.cs` y `Productos/Contratos.cs`. Facturación necesita comprobar actividad, leer precios y llenar selectores; no recibe una API completa para modificar clientes o productos. `ProductoConsultaDto` se reutiliza para catálogo y lectura de precios/estado, sin introducir entidades persistidas en aplicación.

EF DbContext ya tiene responsabilidades de seguimiento y unidad de trabajo. La interfaz adicional existe para sustituir la persistencia en **este caso de uso**, no porque todo acceso a EF deba envolverse. La [guía de persistencia de Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design) explica esa distinción y el papel de interfaces/adaptadores; sus ejemplos no obligan a adoptar microservicios en este proyecto.

## Transacción y ciclo de vida

`ITransaccionFacturacion` es IDisposable con `Confirmar`. El servicio decide cuándo empieza y cuándo confirma; el adaptador decide cómo implementar Serializable. Los tres adaptadores se construyen con el **mismo DbContext** en `Composicion/FacturacionWeb.cs`. Crear cada uno con un contexto distinto rompería el alcance de las lecturas transaccionales, aunque sus interfaces compilaran.

El repositorio no dispone un contexto que no creó. El anfitrión MVC dispone `FacturacionWeb` al terminar la petición; el constructor público de FacturasController que acepta `IServicioFacturacion` deja el ciclo de vida de esa dependencia al llamador. La fachada compatible utiliza el contexto que recibió del consumidor, como antes. No hay contexto estático ni singleton.

La composición usa constructores normales y una función que lee la tasa cuando se necesita. Listados/detalles no requieren leer TasaImpuestos. No se agrega DI nativa ni se modifica Global.asax/Web.config para registrar un contenedor.

## Compatibilidad pendiente de modernizar

`FacturacionOperaciones.Crear` delega al servicio nuevo. Consultar, Obtener, ContarPendientes, Emitir y Anular delegan en infraestructura. Sus firmas públicas se mantienen para que WCF, Windows Service y llamadas del Módulo 2 continúen funcionando sin cambios.

Consultar/Obtener de esa fachada todavía entregan IQueryable/entidades. Es una concesión explícita de compatibilidad; el contrato nuevo no adopta esa fuga de persistencia. Antes de retirar la fachada habría que localizar y adaptar a todos los consumidores, para mantener la compatibilidad de los anfitriones existentes.

## Contratos utilizados en el Módulo 4

`IConsultaProductos` incorpora `Task<List<ProductoConsultaDto>> ListarAsync(CancellationToken)` (activos e inactivos, orden Codigo/Id, máximo 20) y `Task<ProductoConsultaDto> ObtenerAsync(int, CancellationToken)` (null si falta). Los implementa ConsultaProductosEf6, conservando ListarActivos/Obtener(int[]) síncronos y reutilizando la proyección. `IRepositorioFacturas.ContarPendientes()` concentra el conteo usado por la fachada y el Windows Service. Actualizar también implementaciones externas de estos contratos. Contexto y logger se poseen en la composición del anfitrión, no en el dominio.
