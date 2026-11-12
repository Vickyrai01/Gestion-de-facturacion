using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public sealed class DetallesfacturaRepository : Repository<Detallesfactura>, IDetallesfacturaRepository
{
    public DetallesfacturaRepository(FacturacionDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Detallesfactura>> GetByFacturaIdAsync(
        int facturaid,
        CancellationToken cancellationToken = default)
        => await Entities
            .AsNoTracking()
            .Include(detalle => detalle.Producto)
            .Where(detalle => detalle.Facturaid == facturaid)
            .OrderBy(detalle => detalle.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Detallesfactura>> GetByProductoIdAsync(
        int productoid,
        CancellationToken cancellationToken = default)
        => await Entities
            .AsNoTracking()
            .Where(detalle => detalle.Productoid == productoid)
            .OrderByDescending(detalle => detalle.Id)
            .ToListAsync(cancellationToken);
}
