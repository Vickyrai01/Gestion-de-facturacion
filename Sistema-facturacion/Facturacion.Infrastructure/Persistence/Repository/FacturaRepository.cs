using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public sealed class FacturaRepository : Repository<Factura>, IFacturaRepository
{
    public FacturaRepository(FacturacionDbContext context)
        : base(context)
    {
    }

    public Task<Factura?> GetByNumeroAsync(
        string numero,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .Include(factura => factura.Cliente)
            .Include(factura => factura.Detallesfacturas)
            .SingleOrDefaultAsync(
                factura => factura.Numero == numero,
                cancellationToken);

    public Task<Factura?> GetWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .Include(factura => factura.Cliente)
            .Include(factura => factura.Detallesfacturas)
                .ThenInclude(detalle => detalle.Producto)
            .SingleOrDefaultAsync(
                factura => factura.Id == id,
                cancellationToken);
}
