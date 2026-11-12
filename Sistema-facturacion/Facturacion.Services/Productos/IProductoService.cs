using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Services.Productos;

public interface IProductoService
{
    Task<IReadOnlyList<Producto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Producto>> GetActivosAsync(CancellationToken cancellationToken = default);

    Task<Producto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Producto?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task<Producto> CreateAsync(Producto producto, CancellationToken cancellationToken = default);

    Task<Producto> UpdateAsync(Producto producto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
