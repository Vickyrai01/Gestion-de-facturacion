using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using FacturacionLegacy.Datos.Entidades;
using FacturacionLegacy.Datos.Facturacion.Dominio;

namespace FacturacionLegacy.Datos.Facturacion.Infraestructura
{
    public class RepositorioFacturasEf6 : IRepositorioFacturas
    {
        private readonly FacturacionDbContext db;
        public RepositorioFacturasEf6(FacturacionDbContext db) { this.db = db; }

        public ITransaccionFacturacion IniciarCreacion()
        {
            return new TransaccionEf6(db.Database.BeginTransaction(IsolationLevel.Serializable));
        }
        public int Guardar(FacturaEnPreparacion nueva)
        {
            long numero = db.Database.SqlQuery<long>("SELECT NEXT VALUE FOR dbo.NumeroFactura").Single();
            var factura = new Factura
            {
                Numero = "FAC-" + numero.ToString("D10"), ClienteId = nueva.ClienteId, FechaEmision = nueva.FechaEmision,
                Estado = EstadosFactura.Pendiente, Subtotal = nueva.Subtotal, Impuestos = nueva.Impuestos, Total = nueva.Total
            };
            foreach (var detalle in nueva.Detalles)
                factura.Detalles.Add(new DetalleFactura { ProductoId = detalle.ProductoId, Cantidad = detalle.Cantidad, PrecioUnitario = detalle.PrecioUnitario, Subtotal = detalle.Subtotal });
            db.Facturas.Add(factura);
            db.SaveChanges();
            return factura.Id;
        }
        public IList<FacturaResumenDto> Consultar(int? clienteId, string estado)
        {
            return ConsultarEntidades(db, clienteId, estado).Select(f => new FacturaResumenDto
            {
                Id = f.Id, Numero = f.Numero, ClienteId = f.ClienteId, ClienteNombre = f.Cliente.Nombre, ClienteApellido = f.Cliente.Apellido,
                FechaEmision = f.FechaEmision, Estado = f.Estado, Subtotal = f.Subtotal, Impuestos = f.Impuestos, Total = f.Total
            }).ToList();
        }
        public FacturaDetalleDto Obtener(int id)
        {
            var factura = ObtenerEntidad(db, id);
            if (factura == null) return null;
            return new FacturaDetalleDto
            {
                Id = factura.Id, Numero = factura.Numero, ClienteId = factura.ClienteId,
                ClienteNombre = factura.Cliente.Nombre, ClienteApellido = factura.Cliente.Apellido, ClienteDocumento = factura.Cliente.Documento,
                FechaEmision = factura.FechaEmision, Estado = factura.Estado, Subtotal = factura.Subtotal, Impuestos = factura.Impuestos, Total = factura.Total,
                Detalles = factura.Detalles.Select(d => new FacturaLineaDto { ProductoId = d.ProductoId, ProductoCodigo = d.Producto.Codigo, ProductoNombre = d.Producto.Nombre, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal }).ToList()
            };
        }
        public int ContarPendientes()
        {
            return db.Facturas.Count(f => f.Estado == EstadosFactura.Pendiente);
        }
        public bool Existe(int id) { return db.Facturas.Any(f => f.Id == id); }
        public bool Emitir(int id)
        {
            return db.Database.ExecuteSqlCommand("UPDATE dbo.Facturas SET Estado = @p0 WHERE Id = @p1 AND Estado = @p2", EstadosFactura.Emitida, id, EstadosFactura.Pendiente) == 1;
        }
        public bool Anular(int id)
        {
            return db.Database.ExecuteSqlCommand("UPDATE dbo.Facturas SET Estado = @p0 WHERE Id = @p1 AND Estado IN (@p2, @p3)", EstadosFactura.Anulada, id, EstadosFactura.Pendiente, EstadosFactura.Emitida) == 1;
        }
        public IList<ReporteEstadoDto> ResumirPorEstado()
        {
            return db.Facturas.AsNoTracking().GroupBy(f => f.Estado).Select(g => new ReporteEstadoDto { Estado = g.Key, Cantidad = g.Count(), Total = g.Sum(f => f.Total) }).ToList();
        }

        // Solo la fachada anterior usa estas consultas de entidades por compatibilidad.
        internal static IQueryable<Factura> ConsultarEntidades(FacturacionDbContext db, int? clienteId, string estado)
        {
            var query = db.Facturas.AsNoTracking().Include(f => f.Cliente).AsQueryable();
            if (clienteId.HasValue) query = query.Where(f => f.ClienteId == clienteId.Value);
            if (!string.IsNullOrEmpty(estado)) query = query.Where(f => f.Estado == estado);
            return query.OrderByDescending(f => f.FechaEmision).ThenByDescending(f => f.Id);
        }
        internal static Factura ObtenerEntidad(FacturacionDbContext db, int id)
        {
            return db.Facturas.AsNoTracking().Include(f => f.Cliente).Include(f => f.Detalles.Select(d => d.Producto)).SingleOrDefault(f => f.Id == id);
        }

        private sealed class TransaccionEf6 : ITransaccionFacturacion
        {
            private readonly DbContextTransaction transaction;
            public TransaccionEf6(DbContextTransaction transaction) { this.transaction = transaction; }
            public void Confirmar() { transaction.Commit(); }
            public void Dispose() { transaction.Dispose(); }
        }
    }
}
