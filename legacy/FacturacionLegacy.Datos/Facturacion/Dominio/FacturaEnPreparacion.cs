using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FacturacionLegacy.Datos.Facturacion.Dominio
{
    // Cabecera y detalles se construyen juntos; no se aceptan totales externos.
    // No es una entidad mapeada por EF6 ni define un esquema nuevo.
    public sealed class FacturaEnPreparacion
    {
        private const decimal ImporteMaximo = 9999999999999999.99m;
        public int ClienteId { get; private set; }
        public DateTime FechaEmision { get; private set; }
        public ReadOnlyCollection<DetalleCalculado> Detalles { get; private set; }
        public decimal Subtotal { get; private set; }
        public decimal Impuestos { get; private set; }
        public decimal Total { get; private set; }

        private FacturaEnPreparacion() { }

        public static void ValidarSolicitud(IList<LineaFactura> lineas, decimal tasa)
        {
            if (lineas == null || lineas.Count == 0 || lineas.Count > 100)
                throw new ArgumentException("La factura debe contener entre 1 y 100 detalles.");
            if (lineas.Any(l => l == null || l.ProductoId <= 0 || l.Cantidad <= 0 || l.Cantidad > 1000000))
                throw new ArgumentException("Seleccioná productos y cantidades positivas, hasta 1000000 por detalle.");
            if (tasa < 0m || tasa > 1m) throw new ArgumentException("La tasa didáctica debe estar entre 0 y 1.");
        }

        public static FacturaEnPreparacion Crear(int clienteId, IList<LineaFactura> lineas, IDictionary<int, decimal> precios, decimal tasa)
        {
            ValidarSolicitud(lineas, tasa);
            if (clienteId <= 0) throw new ArgumentException("El cliente no existe o está inactivo.");
            var detalles = new List<DetalleCalculado>();
            foreach (var linea in lineas)
            {
                decimal precio;
                if (precios == null || !precios.TryGetValue(linea.ProductoId, out precio) || precio < 0)
                    throw new ArgumentException("Uno de los productos no existe o está inactivo.");
                // Evitar overflow de decimal incluso si se sustituye la persistencia.
                if (precio > ImporteMaximo / linea.Cantidad)
                    throw new ArgumentException("El importe excede la capacidad de la base de datos.");
                detalles.Add(new DetalleCalculado(linea.ProductoId, linea.Cantidad, precio));
            }
            decimal subtotal = detalles.Sum(d => d.Subtotal);
            decimal impuestos = decimal.Round(subtotal * tasa, 2, MidpointRounding.AwayFromZero);
            decimal total = subtotal + impuestos;
            if (total > ImporteMaximo) throw new ArgumentException("El importe excede la capacidad de la base de datos.");
            return new FacturaEnPreparacion
            {
                ClienteId = clienteId, FechaEmision = DateTime.UtcNow, Detalles = detalles.AsReadOnly(),
                Subtotal = subtotal, Impuestos = impuestos, Total = total
            };
        }
    }

    public sealed class DetalleCalculado
    {
        internal DetalleCalculado(int productoId, int cantidad, decimal precio)
        {
            ProductoId = productoId;
            Cantidad = cantidad;
            PrecioUnitario = precio;
            Subtotal = decimal.Round(precio * cantidad, 2, MidpointRounding.AwayFromZero);
        }
        public int ProductoId { get; private set; }
        public int Cantidad { get; private set; }
        public decimal PrecioUnitario { get; private set; }
        public decimal Subtotal { get; private set; }
    }
}
