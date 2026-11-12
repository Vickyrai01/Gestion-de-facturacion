using Facturacion.Infrastructure.Persistence.Models;

namespace Facturacion.Services.Usuarios;

public interface IUsuarioService
{
    Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Usuario> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> GetByNombreusuarioAsync(string nombreusuario, CancellationToken cancellationToken = default);

    Task<Usuario> CreateAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<Usuario> UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
