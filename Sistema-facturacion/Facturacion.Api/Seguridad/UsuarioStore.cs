using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Seguridad;

/// <summary>
/// Store de ASP.NET Core Identity sobre la tabla USUARIOS existente.
///
/// En vez de crear las tablas AspNetUsers/AspNetRoles y copiar los datos, le
/// enseñamos a Identity a leer y escribir el esquema que ya usa el legacy:
/// mismo identificador, mismo nombre de usuario, mismo hash y mismo rol.
///
/// Limitaciones (a propósito, para la clase): el esquema no tiene columnas para
/// email, security stamp ni bloqueo por intentos, así que esos stores no se
/// implementan. Cada usuario tiene exactamente un rol (columna ROL).
/// </summary>
public sealed class UsuarioStore :
    IUserPasswordStore<Usuario>,
    IUserRoleStore<Usuario>
{
    private readonly FacturacionDbContext context;

    public UsuarioStore(FacturacionDbContext context)
    {
        this.context = context;
    }

    // ---- IUserStore ---------------------------------------------------------

    public Task<string> GetUserIdAsync(Usuario user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(Usuario user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.Nombreusuario);

    public Task SetUserNameAsync(Usuario user, string? userName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        user.Nombreusuario = userName.Trim();
        return Task.CompletedTask;
    }

    // No hay columna para el nombre normalizado: se calcula al vuelo.
    public Task<string?> GetNormalizedUserNameAsync(Usuario user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.Nombreusuario.ToUpperInvariant());

    public Task SetNormalizedUserNameAsync(Usuario user, string? normalizedName, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task<IdentityResult> CreateAsync(Usuario user, CancellationToken cancellationToken)
    {
        context.Usuarios.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(Usuario user, CancellationToken cancellationToken)
    {
        context.Usuarios.Update(user);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Usuario user, CancellationToken cancellationToken)
    {
        context.Usuarios.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public Task<Usuario?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var id))
            return Task.FromResult<Usuario?>(null);

        return context.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);
    }

    // Identity busca por nombre normalizado (mayúsculas). La comparación se hace
    // sobre UPPER(NOMBREUSUARIO) para que "admin" y "Admin" sean la misma cuenta.
    public Task<Usuario?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => context.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(
                usuario => usuario.Nombreusuario.ToUpper() == normalizedUserName,
                cancellationToken);

    // ---- IUserPasswordStore -------------------------------------------------

    public Task SetPasswordHashAsync(Usuario user, string? passwordHash, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        user.Passwordhash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(Usuario user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.Passwordhash);

    public Task<bool> HasPasswordAsync(Usuario user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(user.Passwordhash));

    // ---- IUserRoleStore (un único rol por usuario) --------------------------

    public Task AddToRoleAsync(Usuario user, string roleName, CancellationToken cancellationToken)
    {
        if (!Roles.EsValido(roleName))
            throw new ArgumentException($"Rol desconocido: '{roleName}'.", nameof(roleName));

        user.Rol = roleName.Trim().ToUpperInvariant();
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(Usuario user, string roleName, CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "Cada usuario tiene exactamente un rol: usar AddToRoleAsync para reemplazarlo.");

    public Task<IList<string>> GetRolesAsync(Usuario user, CancellationToken cancellationToken)
        => Task.FromResult<IList<string>>([user.Rol]);

    public Task<bool> IsInRoleAsync(Usuario user, string roleName, CancellationToken cancellationToken)
        => Task.FromResult(string.Equals(user.Rol, roleName, StringComparison.OrdinalIgnoreCase));

    public async Task<IList<Usuario>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        => await context.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Rol == roleName)
            .ToListAsync(cancellationToken);

    public void Dispose()
    {
        // El DbContext lo administra el contenedor de dependencias.
    }
}
