using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public sealed class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(FacturacionDbContext context)
        : base(context)
    {
    }

    public Task<Usuario?> GetByNombreusuarioAsync(
        string nombreusuario,
        CancellationToken cancellationToken = default)
        => Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                usuario => usuario.Nombreusuario == nombreusuario,
                cancellationToken);
}
