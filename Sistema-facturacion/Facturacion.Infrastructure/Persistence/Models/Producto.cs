using System;
using System.Collections.Generic;

namespace Facturacion.Infrastructure.Persistence.Models;

public partial class Producto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Preciounitario { get; set; }

    public int Stock { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Detallesfactura> Detallesfacturas { get; set; } = new List<Detallesfactura>();
}
