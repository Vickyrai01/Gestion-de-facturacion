using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Linq;
using FacturacionLegacy.Datos.Entidades;

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
        private const decimal ImporteMaximo = 9999999999999999.99m;

        public static int Crear(FacturacionDbContext db, int clienteId, IList<LineaFactura> lineas, decimal tasa)
        {
            if (lineas == null || lineas.Count == 0 || lineas.Count > 100)
                throw new ArgumentException("La factura debe contener entre 1 y 100 detalles.");
            if (lineas.Any(l => l == null || l.ProductoId <= 0 || l.Cantidad <= 0 || l.Cantidad > 1000000))
                throw new ArgumentException("Seleccioná productos y cantidades positivas, hasta 1000000 por detalle.");
            if (tasa < 0m || tasa > 1m) throw new ArgumentException("La tasa didáctica debe estar entre 0 y 1.");

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                if (!db.Clientes.Any(c => c.Id == clienteId && c.Activo))
                    throw new ArgumentException("El cliente no existe o está inactivo.");
                int[] ids = lineas.Select(l => l.ProductoId).Distinct().ToArray();
                var productos = db.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);
                if (productos.Count != ids.Length || productos.Values.Any(p => !p.Activo))
                    throw new ArgumentException("Uno de los productos no existe o está inactivo.");

                var factura = new Factura { ClienteId = clienteId, Estado = EstadosFactura.Pendiente, FechaEmision = DateTime.UtcNow };
                foreach (var linea in lineas)
                {
                    decimal precio = productos[linea.ProductoId].PrecioUnitario;
                    factura.Detalles.Add(new DetalleFactura { ProductoId = linea.ProductoId, Cantidad = linea.Cantidad, PrecioUnitario = precio, Subtotal = decimal.Round(precio * linea.Cantidad, 2, MidpointRounding.AwayFromZero) });
                }
                factura.Subtotal = factura.Detalles.Sum(d => d.Subtotal);
                factura.Impuestos = decimal.Round(factura.Subtotal * tasa, 2, MidpointRounding.AwayFromZero);
                factura.Total = factura.Subtotal + factura.Impuestos;
                if (factura.Total > ImporteMaximo) throw new ArgumentException("El importe excede la capacidad de la base de datos.");

                long numero = db.Database.SqlQuery<long>("SELECT NEXT VALUE FOR dbo.NumeroFactura").Single();
                factura.Numero = "FAC-" + numero.ToString("D10");
                db.Facturas.Add(factura);
                db.SaveChanges();
                transaction.Commit();
                return factura.Id;
            }
        }

        public static IQueryable<Factura> Consultar(FacturacionDbContext db, int? clienteId, string estado)
        {
            var query = db.Facturas.AsNoTracking().Include(f => f.Cliente).AsQueryable();
            if (clienteId.HasValue) query = query.Where(f => f.ClienteId == clienteId.Value);
            if (!string.IsNullOrEmpty(estado)) query = query.Where(f => f.Estado == estado);
            return query.OrderByDescending(f => f.FechaEmision).ThenByDescending(f => f.Id);
        }
        public static Factura Obtener(FacturacionDbContext db, int id)
        {
            return db.Facturas.AsNoTracking().Include(f => f.Cliente).Include(f => f.Detalles.Select(d => d.Producto)).SingleOrDefault(f => f.Id == id);
        }
        public static int ContarPendientes(FacturacionDbContext db)
        {
            return db.Facturas.Count(f => f.Estado == EstadosFactura.Pendiente);
        }
        public static bool Emitir(FacturacionDbContext db, int id)
        {
            return db.Database.ExecuteSqlCommand("UPDATE dbo.Facturas SET Estado = @p0 WHERE Id = @p1 AND Estado = @p2", EstadosFactura.Emitida, id, EstadosFactura.Pendiente) == 1;
        }
        public static bool Anular(FacturacionDbContext db, int id)
        {
            return db.Database.ExecuteSqlCommand("UPDATE dbo.Facturas SET Estado = @p0 WHERE Id = @p1 AND Estado IN (@p2, @p3)", EstadosFactura.Anulada, id, EstadosFactura.Pendiente, EstadosFactura.Emitida) == 1;
        }
    }
}
