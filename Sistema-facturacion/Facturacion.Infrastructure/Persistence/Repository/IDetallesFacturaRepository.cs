using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IDetallesfacturaRepository : IRepository<Detallesfactura>
{
    Task<IReadOnlyList<Detallesfactura>> GetByFacturaIdAsync(
        int facturaid,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Detallesfactura>> GetByProductoIdAsync(
        int productoid,
        CancellationToken cancellationToken = default);
}
