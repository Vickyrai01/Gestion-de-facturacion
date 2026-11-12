using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IProductoRepository : IRepository<Producto>
{
    Task<Producto?> GetByCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Producto>> GetActivosAsync(
        CancellationToken cancellationToken = default);
}
