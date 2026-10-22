# Arquitectura y modernización

El Módulo 3 separa dominio, aplicación, contratos e infraestructura. El Módulo 4 utiliza esos límites para demostrar herramientas de modernización sin cambiar las reglas de facturación.

| Concepto | Componente del sistema | Qué observar en clase |
| --- | --- | --- |
| Async / await | `Productos/ConsultaProductosEf6.cs` | ToListAsync y FirstOrDefaultAsync detrás de IConsultaProductos, reutilizando ProductoConsultaDto. |
| DI | `Facturacion/Aplicacion/ServicioFacturacion.cs` | Dependencias recibidas por constructor y sustitución por dobles de prueba. |
| Composición manual | `Composicion/FacturacionWeb.cs` y `WindowsService/ComposicionProcesos.cs` | Propiedad y duración del contexto por petición u operación. |
| Background jobs | `WindowsService/ResumenPendientes.cs` | Separación entre programación periódica y consulta de pendientes. |
| Logging | `WindowsService/RegistroProcesamiento.cs` | Configuración del proveedor en el anfitrión y propiedades estructuradas. |

Las rutas de Productos y Facturacion parten de `legacy/FacturacionLegacy.Datos/`; Composicion pertenece al proyecto web y WindowsService a `legacy/FacturacionLegacy.WindowsService/`.

Para la demostración completa, seguir [arquitectura aplicada a la modernización](../modulo-4/integracion-modulo-3.md) y las [pruebas de arquitectura](validacion.md). La composición sigue siendo compatible con MVC 5 y .NET Framework 4.8; el contenedor nativo de ASP.NET Core se estudia como comparación para .NET 10.
