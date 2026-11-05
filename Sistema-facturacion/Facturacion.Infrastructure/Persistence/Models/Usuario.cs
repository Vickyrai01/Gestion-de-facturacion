using System;
using System.Collections.Generic;

namespace Facturacion.Infrastructure.Persistence.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombreusuario { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public bool Activo { get; set; }
}
