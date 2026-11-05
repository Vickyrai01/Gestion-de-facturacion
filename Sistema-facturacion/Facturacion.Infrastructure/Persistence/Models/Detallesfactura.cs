using System;
using System.Collections.Generic;

namespace Facturacion.Infrastructure.Persistence.Models;

public partial class Detallesfactura
{
    public int Id { get; set; }

    public int Facturaid { get; set; }

    public int Productoid { get; set; }

    public int Cantidad { get; set; }

    public decimal Preciounitario { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Factura Factura { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
