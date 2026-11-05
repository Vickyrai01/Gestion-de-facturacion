using System;
using System.Collections.Generic;

namespace Facturacion.Infrastructure.Persistence.Models;

public partial class Factura
{
    public int Id { get; set; }

    public string Numero { get; set; } = null!;

    public int Clienteid { get; set; }

    public DateTime Fechaemision { get; set; }

    public string Estado { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal Impuestos { get; set; }

    public decimal Total { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<Detallesfactura> Detallesfacturas { get; set; } = new List<Detallesfactura>();
}
