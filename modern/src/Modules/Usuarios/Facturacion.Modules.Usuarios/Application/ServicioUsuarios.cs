using Facturacion.BuildingBlocks.Resultados;
using Facturacion.Modules.Usuarios.Domain;

namespace Facturacion.Modules.Usuarios.Application;

/// <summary>
/// DTO público de usuario.
/// IMPORTANTE (Desacople DTO / Repositorios): NUNCA incluye `PasswordHash`.
/// En la Clase 5/6 `UsuariosController` devolvía la entidad `Models.Usuario` directamente,
/// filtrando el `Passwordhash` en las respuestas JSON.
/// </summary>
public sealed record UsuarioDto(
    int Id,
    string NombreUsuario,
    string Rol,
    bool Activo);

public sealed record LoginComando(string? NombreUsuario, string? Password);

public sealed record LoginResultadoDto(
    string Token,
    DateTime ExpiraUtc,
    UsuarioDto Usuario);

public sealed record CrearUsuarioComando(
    string? NombreUsuario,
    string? Password,
    string? Rol);

public sealed record ActualizarUsuarioComando(
    string? Rol,
    string? NuevoPassword,
    bool? Activo);

public interface IServicioUsuarios
{
    Task<Result<LoginResultadoDto>> LoginAsync(LoginComando comando, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Result<UsuarioDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<UsuarioDto>> CrearAsync(CrearUsuarioComando comando, CancellationToken cancellationToken = default);

    Task<Result<UsuarioDto>> ActualizarAsync(int id, ActualizarUsuarioComando comando, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(int id, CancellationToken cancellationToken = default);
}

internal sealed class ServicioUsuarios(
    IRepositorioUsuarios repositorio,
    IHasherPassword hasher,
    IControlIntentosLogin controlIntentos,
    IEmisorToken emisorToken) : IServicioUsuarios
{
    // Hash ficticio calculado una vez para igualar tiempos de respuesta cuando el usuario no existe
    // (misma defensa contra timing-attacks de AccountController.HashInexistente en el legacy).
    private readonly string hashInexistente =
        hasher.CrearHash("ClaveFicticiaAntiTimingAttack_123!").Valor;

    public async Task<Result<LoginResultadoDto>> LoginAsync(
        LoginComando comando,
        CancellationToken cancellationToken = default)
    {
        var nombreNormalizado = Usuario.NormalizarNombre(comando.NombreUsuario);
        if (nombreNormalizado is null || string.IsNullOrEmpty(comando.Password))
            return ErroresUsuario.CredencialesInvalidas;

        if (!controlIntentos.AdmitirIntento(nombreNormalizado))
            return ErroresUsuario.IntentosExcedidos;

        var usuario = await repositorio.ObtenerPorNombreAsync(nombreNormalizado, cancellationToken);
        var hashAVerificar = usuario?.PasswordHash ?? hashInexistente;
        var passwordValida = hasher.Verificar(comando.Password, hashAVerificar);

        if (!passwordValida || usuario is null || !usuario.Activo)
            return ErroresUsuario.CredencialesInvalidas;

        controlIntentos.Limpiar(nombreNormalizado);
        var token = emisorToken.Emitir(usuario);
        return new LoginResultadoDto(token.Token, token.ExpiraUtc, ADto(usuario));
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await repositorio.ListarAsync(cancellationToken);
        return usuarios.Select(ADto).ToList();
    }

    public async Task<Result<UsuarioDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        return usuario is null ? ErroresUsuario.NoEncontrado(id) : ADto(usuario);
    }

    public async Task<Result<UsuarioDto>> CrearAsync(
        CrearUsuarioComando comando,
        CancellationToken cancellationToken = default)
    {
        var hashResultado = hasher.CrearHash(comando.Password);
        if (hashResultado.EsFallo)
            return hashResultado.Error;

        var creacion = Usuario.Crear(comando.NombreUsuario, hashResultado.Valor, comando.Rol);
        if (creacion.EsFallo)
            return creacion.Error;

        var usuario = creacion.Valor;
        if (await repositorio.ExisteNombreAsync(usuario.NombreUsuario, exceptoId: 0, cancellationToken))
            return ErroresUsuario.NombreDuplicado(usuario.NombreUsuario);

        await repositorio.AgregarAsync(usuario, cancellationToken);
        await repositorio.GuardarCambiosAsync(cancellationToken);
        return ADto(usuario);
    }

    public async Task<Result<UsuarioDto>> ActualizarAsync(
        int id,
        ActualizarUsuarioComando comando,
        CancellationToken cancellationToken = default)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return ErroresUsuario.NoEncontrado(id);

        if (!string.IsNullOrWhiteSpace(comando.Rol))
        {
            var cambioRol = usuario.CambiarRol(comando.Rol);
            if (cambioRol.EsFallo)
                return cambioRol.Error;
        }

        if (!string.IsNullOrWhiteSpace(comando.NuevoPassword))
        {
            var nuevoHash = hasher.CrearHash(comando.NuevoPassword);
            if (nuevoHash.EsFallo)
                return nuevoHash.Error;

            usuario.CambiarPasswordHash(nuevoHash.Valor);
        }

        if (comando.Activo.HasValue)
        {
            if (comando.Activo.Value)
                usuario.Activar();
            else
                usuario.Desactivar();
        }

        await repositorio.GuardarCambiosAsync(cancellationToken);
        return ADto(usuario);
    }

    public async Task<Result> DesactivarAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return ErroresUsuario.NoEncontrado(id);

        usuario.Desactivar();
        await repositorio.GuardarCambiosAsync(cancellationToken);
        return Result.Exito();
    }

    internal static UsuarioDto ADto(Usuario usuario) =>
        new(usuario.Id, usuario.NombreUsuario, usuario.Rol, usuario.Activo);
}

