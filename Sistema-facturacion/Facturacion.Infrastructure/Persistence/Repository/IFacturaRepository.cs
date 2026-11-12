using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IFacturaRepository : IRepository<Factura>
{
    Task<Factura?> GetByNumeroAsync(
        string numero,
        CancellationToken cancellationToken = default);

    Task<Factura?> GetWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);
}
