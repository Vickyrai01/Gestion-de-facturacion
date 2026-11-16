using System.Text;
using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace Facturacion.Api.Seguridad;

/// <summary>
/// Hasher de Identity que además entiende los hashes BCrypt del sistema legacy.
///
/// - Hashes nuevos: se generan con el formato de Identity (PBKDF2, versión 3).
/// - Hash BCrypt: se verifica con BCrypt. Si la contraseña es correcta devuelve
///   <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> y UserManager
///   reemplaza el hash por uno de Identity usando la contraseña que el usuario
///   acaba de ingresar. Así se migra la cuenta "al iniciar sesión", sin pedirle
///   al usuario que cree una cuenta nueva ni que restablezca su contraseña.
///
/// No se puede convertir un hash BCrypt en uno PBKDF2 sin la contraseña: por eso
/// la migración ocurre recién cuando el usuario vuelve a entrar.
/// </summary>
public sealed class PasswordHasherCompatibleLegacy : IPasswordHasher<Usuario>
{
    // bcrypt solo considera los primeros 72 bytes; el legacy rechazaba contraseñas más largas.
    private const int MaximoBytesBcrypt = 72;

    private readonly PasswordHasher<Usuario> identityHasher;

    public PasswordHasherCompatibleLegacy(PasswordHasher<Usuario> identityHasher)
    {
        this.identityHasher = identityHasher;
    }

    /// <summary>Indica si el hash todavía está en el formato del sistema legacy.</summary>
    public static bool EsHashLegacy(string? hash)
        => hash is not null
           && (hash.StartsWith("$2a$", StringComparison.Ordinal)
               || hash.StartsWith("$2b$", StringComparison.Ordinal)
               || hash.StartsWith("$2y$", StringComparison.Ordinal));

    public string HashPassword(Usuario user, string password)
        => identityHasher.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(
        Usuario user,
        string hashedPassword,
        string providedPassword)
    {
        if (!EsHashLegacy(hashedPassword))
            return identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        if (string.IsNullOrEmpty(providedPassword)
            || Encoding.UTF8.GetByteCount(providedPassword) > MaximoBytesBcrypt)
            return PasswordVerificationResult.Failed;

        try
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
