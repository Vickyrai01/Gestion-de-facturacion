using System.Security.Claims;
using System.Text;
using Facturacion.BuildingBlocks.Aplicacion;
using Facturacion.BuildingBlocks.Resultados;
using Facturacion.Modules.Usuarios.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Facturacion.Modules.Usuarios.Infrastructure;

public sealed class OpcionesJwt
{
    public const string Seccion = "Autenticacion:Jwt";

    public string Issuer { get; set; } = "Facturacion.MonolitoModular";
    public string Audience { get; set; } = "facturacion-api";
    public string ClaveSimetrica { get; set; } = "ClaveDidacticaClase7MonolitoModularNet10_Minimo32Bytes!";
    public int MinutosExpiracion { get; set; } = 120;
}

/// <summary>
/// Adaptador de hashing BCrypt compatible con `PasswordSeguridad.cs` del legacy.
/// Exige entre 12 caracteres y 72 bytes UTF-8 y verifica hashes `$2a$` / `$2b$` existentes en la base.
/// </summary>
internal sealed class HasherPasswordBcrypt(int workFactor = 10) : IHasherPassword
{
    public Result<string> CrearHash(string? passwordPlano)
    {
        if (string.IsNullOrEmpty(passwordPlano)
            || passwordPlano.Length < 12
            || Encoding.UTF8.GetByteCount(passwordPlano) > 72)
        {
            return ErroresUsuario.PasswordInvalida;
        }

        return BCrypt.Net.BCrypt.HashPassword(passwordPlano, workFactor);
    }

    public bool Verificar(string? passwordPlano, string? passwordHash)
    {
        if (string.IsNullOrEmpty(passwordPlano)
            || Encoding.UTF8.GetByteCount(passwordPlano) > 72
            || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(passwordPlano, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

/// <summary>
/// Adaptador de ventana de intentos fallidos de login (5 intentos en 15 minutos, hasta 10.000 cuentas),
/// migrado de `legacyGestionDeFacturacion.web/Seguridad/IntentosLogin.cs`, pero inyectando <see cref="IReloj"/>
/// para que sea determinista y testeable sin bloquear hilos ni depender de estado estático global.
/// </summary>
internal sealed class ControlIntentosLoginEnMemoria(IReloj reloj) : IControlIntentosLogin
{
    private sealed class Ventana
    {
        public DateTime HastaUtc;
        public int Cantidad;
    }

    private readonly object sincronizacion = new();
    private readonly Dictionary<string, Ventana> intentos = new(StringComparer.Ordinal);

    public bool AdmitirIntento(string nombreNormalizado)
    {
        lock (sincronizacion)
        {
            var ahora = reloj.AhoraUtc;
            foreach (var vencida in intentos.Where(p => p.Value.HastaUtc <= ahora).Select(p => p.Key).ToList())
                intentos.Remove(vencida);

            if (!intentos.TryGetValue(nombreNormalizado, out var ventana))
            {
                if (intentos.Count >= 10_000)
                    return false;

                ventana = new Ventana { HastaUtc = ahora.AddMinutes(15) };
                intentos.Add(nombreNormalizado, ventana);
            }

            if (ventana.Cantidad >= 5)
                return false;

            ventana.Cantidad++;
            return true;
        }
    }

    public void Limpiar(string nombreNormalizado)
    {
        lock (sincronizacion)
        {
            intentos.Remove(nombreNormalizado);
        }
    }
}

/// <summary>
/// Emisor de JWT Bearer para la API moderna.
/// Reemplaza a `FormsAuthentication.SetAuthCookie` + `Application_PostAuthenticateRequest` de `Global.asax.cs`.
/// </summary>
internal sealed class EmisorJwtSimetrico(IOptions<OpcionesJwt> opciones, IReloj reloj) : IEmisorToken
{
    public TokenEmitido Emitir(Usuario usuario)
    {
        var cfg = opciones.Value;
        var ahora = reloj.AhoraUtc;
        var expira = ahora.AddMinutes(Math.Clamp(cfg.MinutosExpiracion, 5, 1440));
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.ClaveSimetrica));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = cfg.Issuer,
            Audience = cfg.Audience,
            NotBefore = ahora,
            IssuedAt = ahora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", usuario.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Role, usuario.Rol)
            ])
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);
        return new TokenEmitido(token, expira);
    }
}

