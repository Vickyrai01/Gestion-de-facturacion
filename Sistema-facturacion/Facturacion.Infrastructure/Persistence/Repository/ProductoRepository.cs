using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public sealed class ProductoRepository : Repository<Producto>, IProductoRepository
{
    public ProductoRepository(FacturacionDbContext context)
        : base(context)
    {
    }

    public Task<Producto?> GetByCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                producto => producto.Codigo == codigo,
                cancellationToken);

    public async Task<IReadOnlyList<Producto>> GetActivosAsync(
        CancellationToken cancellationToken = default)
        => await Entities
            .AsNoTracking()
            .Where(producto => producto.Activo)
            .OrderBy(producto => producto.Nombre)
            .ToListAsync(cancellationToken);
}
