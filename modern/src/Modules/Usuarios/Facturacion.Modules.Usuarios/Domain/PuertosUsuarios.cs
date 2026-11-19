using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Usuarios.Domain;

public interface IRepositorioUsuarios
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorNombreAsync(string nombreNormalizado, CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(string nombreNormalizado, int exceptoId = 0, CancellationToken cancellationToken = default);

    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Puerto de hashing de contraseñas.
/// Reemplaza a la clase estática `PasswordSeguridad` del legacy para poder inyectarla.
/// </summary>
public interface IHasherPassword
{
    Result<string> CrearHash(string? passwordPlano);

    bool Verificar(string? passwordPlano, string? passwordHash);
}

/// <summary>
/// Puerto para controlar intentos fallidos de inicio de sesión.
/// Reemplaza a la clase estática `IntentosLogin` del legacy (`legacy/legacyGestionDeFacturacion.web/Seguridad/IntentosLogin.cs`).
/// </summary>
public interface IControlIntentosLogin
{
    bool AdmitirIntento(string nombreNormalizado);

    void Limpiar(string nombreNormalizado);
}

public sealed record TokenEmitido(string Token, DateTime ExpiraUtc);

public interface IEmisorToken
{
    TokenEmitido Emitir(Usuario usuario);
}

