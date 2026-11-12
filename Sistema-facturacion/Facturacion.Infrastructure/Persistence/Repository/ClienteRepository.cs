using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public sealed class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(FacturacionDbContext context) : base(context) { }

    public async Task<Cliente?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default)
        => await Entities.AsNoTracking().FirstOrDefaultAsync(c => c.Documento == documento, cancellationToken);
}