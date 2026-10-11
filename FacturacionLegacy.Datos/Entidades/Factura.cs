using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FacturacionLegacy.Datos.Entidades
{
    public class Factura
    {
        public Factura() { Detalles = new List<DetalleFactura>(); }
        public int Id { get; set; }
        [Required, StringLength(30)]
        public string Numero { get; set; }
        public int ClienteId { get; set; }
        public DateTime FechaEmision { get; set; }
        [Required, StringLength(10)]
        public string Estado { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public virtual Cliente Cliente { get; set; }
        public virtual ICollection<DetalleFactura> Detalles { get; set; }
    }
    public static class EstadosFactura
    {
        public const string Pendiente = "PENDIENTE";
        public const string Emitida = "EMITIDA";
        public const string Anulada = "ANULADA";
        public static readonly string[] Todos = { Pendiente, Emitida, Anulada };
    }
}
