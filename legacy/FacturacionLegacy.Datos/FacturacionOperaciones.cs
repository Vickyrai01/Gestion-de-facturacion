using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FacturacionLegacy.Datos.Entidades;
using FacturacionLegacy.Datos.Clientes;
using FacturacionLegacy.Datos.Productos;
using FacturacionLegacy.Datos.Facturacion;
using FacturacionLegacy.Datos.Facturacion.Aplicacion;
using FacturacionLegacy.Datos.Facturacion.Infraestructura;

namespace FacturacionLegacy.Datos
{
    public class LineaFactura
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un producto.")]
        public int ProductoId { get; set; }
        [Range(1, 1000000, ErrorMessage = "La cantidad debe estar entre 1 y 1000000.")]
        public int Cantidad { get; set; }
    }

    public static class FacturacionOperaciones
    {
        public static int Crear(FacturacionDbContext db, int clienteId, IList<LineaFactura> lineas, decimal tasa)
        {
            // Fachada de compatibilidad: no cambia la firma pública del Módulo 2.
            var servicio = new ServicioFacturacion(new RepositorioFacturasEf6(db), new ConsultaClientesEf6(db), new ConsultaProductosEf6(db), tasa);
            return servicio.Crear(new CrearFacturaRequest { ClienteId = clienteId, Detalles = lineas });
        }

        public static IQueryable<Factura> Consultar(FacturacionDbContext db, int? clienteId, string estado)
        {
            return RepositorioFacturasEf6.ConsultarEntidades(db, clienteId, estado);
        }
        public static Factura Obtener(FacturacionDbContext db, int id)
        {
            return RepositorioFacturasEf6.ObtenerEntidad(db, id);
        }
        public static int ContarPendientes(FacturacionDbContext db)
        {
            return new RepositorioFacturasEf6(db).ContarPendientes();
        }
        public static bool Emitir(FacturacionDbContext db, int id)
        {
            return new RepositorioFacturasEf6(db).Emitir(id);
        }
        public static bool Anular(FacturacionDbContext db, int id)
        {
            return new RepositorioFacturasEf6(db).Anular(id);
        }
    }
}
