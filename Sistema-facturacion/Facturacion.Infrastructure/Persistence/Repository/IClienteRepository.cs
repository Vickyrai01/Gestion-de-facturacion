using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default);
}