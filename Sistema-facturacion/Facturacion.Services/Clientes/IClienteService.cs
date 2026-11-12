using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Services.Clientes;

public interface IClienteService
{
    Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Cliente> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Cliente?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default);

    Task<Cliente> CreateAsync(Cliente cliente, CancellationToken cancellationToken = default);

    Task<Cliente> UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
