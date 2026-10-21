# Diagnóstico de arquitectura antes del refactor

Este diagnóstico analiza el diseño presentado en el Módulo 2 y explica las responsabilidades que separa el Módulo 3. Las rutas de código parten de `legacy/`.

## Responsabilidades actuales

| Componente | Evidencia real | Consecuencia |
| --- | --- | --- |
| `legacyGestionDeFacturacion.web/Controllers/FacturasController.cs` | `Index`, `Reportes` y `PrepararFormulario` consultan DbSet/EF6; `ObtenerTasa` interpreta Web.config; `Emitir` y `Anular` consultan existencia y deciden HTTP. | HTTP, consultas y configuración se concentran en el mismo controller. Sustituir persistencia para probar una acción requiere cambios. |
| `FacturacionLegacy.Datos/FacturacionOperaciones.cs` | `Crear` valida entradas, consulta clientes/productos, calcula importes, obtiene una secuencia SQL y controla la transacción. | La creación ya está fuera del controller, pero las reglas y cálculos dependen de un DbContext concreto. No se puede probar el cálculo aisladamente sin separar esas responsabilidades. |
| `Views/Facturas/Index.cshtml` y `Details.cshtml` | Sus modelos son entidades `Entidades.Factura` y recorren navegaciones Cliente/Producto. | La presentación conoce el modelo de persistencia y sus relaciones. |
| `FacturasController.cs` | Contiene además la clase `ReporteEstado`, utilizada como modelo de `Views/Facturas/Reportes.cshtml`. | El contrato de presentación está definido dentro del archivo del controller. |
| `ClientesController.cs` | Listado, alta, edición y desactivación acceden directamente a EF6; se entregan entidades a las vistas. | CRUD acoplado a persistencia, pendiente para una evolución posterior; no es necesario refactorizarlo completo para enseñar el flujo representativo. |
| `ProductosController.cs` | Normaliza códigos, valida unicidad, redondea el precio y maneja DbUpdateException/SqlException dentro del controller. | Combina HTTP, reglas y errores de almacenamiento. Se documenta; no se cambia ese CRUD en este módulo para limitar conflictos con el Módulo 4. |
| `AccountController.cs` y `Global.asax.cs` | Construyen DbContext para autenticar y recuperar el rol vigente. | Usuarios sigue acoplado a EF6/System.Web. Se conserva el comportamiento y no se modifica autenticación en este refactor. |

## Límites que ya estaban bien resueltos

- Los importes de facturas **no se calculaban en el controller**. `FacturacionOperaciones.Crear` ya usa decimal, precios de la base, redondeo explícito, secuencia SQL y una transacción Serializable.
- `FacturaCrearModel` ya limita la entrada a cliente y líneas, sin aceptar totales ni precios como autoridad.
- `FacturacionLegacy.Wcf/Contratos.cs` ya define DTOs SOAP separados. El servicio mapea entidades a esos contratos; no serializa DbContext ni grafos EF completos.
- `FacturacionLegacy.WindowsService/ResumenPendientes.cs` libera el contexto, evita solapamientos y solo consulta pendientes. Su núcleo se comparte entre consola y servicio.
- Los proyectos WCF y WindowsService ya dependen de Datos, sin referencia al proyecto MVC.
- Autorización y antiforgery se aplican en el servidor; no deben trasladarse a la visibilidad de botones.

## Acoplamiento y duplicación concretos

El constructor implícito de `FacturasController` crea directamente un DbContext y sus acciones llaman métodos estáticos. La dependencia no puede sustituirse por un doble de prueba mediante un constructor. `FacturacionOperaciones.Consultar` devuelve IQueryable de entidades: el consumidor queda relacionado con la consulta diferida y su contexto.

Los selectores de clientes de `Index` y `PrepararFormulario` repiten consulta/ordenación/proyección, aunque difieren en el filtro de activos. Se pueden reunir en una consulta de clientes con una opción explícita. WCF repite algunas asignaciones entre DTO de detalle y resumen, pero sus contratos son públicos: esa semejanza no justifica cambiar SOAP ni refactorizar todo el servicio. No se identificó una segunda política de impuestos real; agregar tipos de impuestos nuevos cambiaría el negocio.

## Alcance seleccionado

Separar facturación en contratos, aplicación, dominio e infraestructura dentro del proyecto existente Datos; agregar consultas pequeñas de Clientes y Productos para sus catálogos. El controller utilizará un servicio sustituible y DTOs. La fachada pública `FacturacionOperaciones` conservará sus firmas para los consumidores existentes. No se crean proyectos, tablas, migraciones, rutas ni contenedores de DI nuevos.

La separación será lógica dentro del mismo ensamblado: no se presentará como una implementación completa de Clean Architecture ni como módulos con aislamiento garantizado por el compilador. Los CRUD de clientes/productos y autenticación conservan sus límites actuales para que el cambio sea acotado y revisable.
