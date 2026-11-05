using System;
using System.Collections.Generic;

namespace Facturacion.Infrastructure.Persistence.Models;

public partial class Cliente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Documento { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}
