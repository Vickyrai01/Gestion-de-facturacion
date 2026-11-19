using Facturacion.BuildingBlocks.Aplicacion;

namespace Facturacion.BuildingBlocks.Infrastructure;

public sealed class RelojSistema : IReloj
{
    public DateTime AhoraUtc => DateTime.UtcNow;
}

