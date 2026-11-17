using System.Text;
using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace Facturacion.Api.Seguridad;

public sealed class PasswordHasherCompatibleLegacy : IPasswordHasher<Usuario>
{
    // bcrypt solo considera los primeros 72 bytes; el legacy rechazaba contraseñas más largas.
    private const int MaximoBytesBcrypt = 72;

    private readonly PasswordHasher<Usuario> identityHasher;

    public PasswordHasherCompatibleLegacy(PasswordHasher<Usuario> identityHasher)
    {
        this.identityHasher = identityHasher;
    }

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
