using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByNombreusuarioAsync(
        string nombreusuario,
        CancellationToken cancellationToken = default);
}
