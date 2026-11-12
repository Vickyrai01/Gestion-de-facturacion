using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Infrastructure.Persistence.Repository;

namespace Facturacion.Services.Usuarios;

public sealed class UsuarioService : IUsuarioService
{
    private readonly IUnitOfWork unitOfWork;

    public UsuarioService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken = default)
        => unitOfWork.Usuarios.GetAllAsync(cancellationToken);

    public async Task<Usuario> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con id {id}.");

    public Task<Usuario?> GetByNombreusuarioAsync(
        string nombreusuario,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreusuario);
        return unitOfWork.Usuarios.GetByNombreusuarioAsync(nombreusuario.Trim(), cancellationToken);
    }

    public async Task<Usuario> CreateAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        Validate(usuario);

        if (await GetByNombreusuarioAsync(usuario.Nombreusuario, cancellationToken) is not null)
            throw new InvalidOperationException(
                $"Ya existe un usuario con el nombre '{usuario.Nombreusuario}'.");

        await unitOfWork.Usuarios.AddAsync(usuario, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    public async Task<Usuario> UpdateAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        Validate(usuario);
        _ = await GetByIdAsync(usuario.Id, cancellationToken);

        var usuarioConMismoNombre = await GetByNombreusuarioAsync(usuario.Nombreusuario, cancellationToken);
        if (usuarioConMismoNombre is not null && usuarioConMismoNombre.Id != usuario.Id)
            throw new InvalidOperationException(
                $"Ya existe un usuario con el nombre '{usuario.Nombreusuario}'.");

        unitOfWork.Usuarios.Update(usuario);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await GetByIdAsync(id, cancellationToken);
        unitOfWork.Usuarios.Delete(usuario);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(Usuario usuario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario.Nombreusuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario.Passwordhash);
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario.Rol);
    }
}
