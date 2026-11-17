using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Facturacion.Api.Seguridad;

/// <summary>
/// Configuración de seguridad de Facturación.
///
/// La autenticación se delega a un sistema externo (ARCA).
///
/// Facturación NO:
/// - Solicita usuario y contraseña.
/// - Genera tokens.
/// - Administra sesiones mediante cookies.
/// - Verifica hashes de contraseñas.
///
/// Facturación SÍ:
/// - Recibe access tokens mediante Authorization: Bearer.
/// - Valida que el token provenga del emisor autorizado.
/// - Comprueba firma, audiencia y vencimiento.
/// - Obtiene la identidad y los roles del usuario.
/// - Aplica políticas de autorización.
/// </summary>
public static class SeguridadExtensions
{
    public static IServiceCollection AddSeguridad(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --------------------------------------------------
        // 1. OBTENER CONFIGURACIÓN DEL PROVEEDOR
        // --------------------------------------------------

        var oidc = configuration.GetSection("Autenticacion:Oidc");

        var authority = oidc["Authority"];
        var audience = oidc["Audience"];

        // No permitimos iniciar la API sin un proveedor
        // configurado. Evitamos aceptar tokens de un origen
        // desconocido o con una audiencia incorrecta.
        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException(
                "Falta configurar Autenticacion:Oidc:Authority.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Falta configurar Autenticacion:Oidc:Audience.");
        }

        // Configuración de los nombres de claims.
        // Deben coincidir con los que entrega ARCA.
        var nameClaimType =
            oidc["NameClaimType"] ?? "preferred_username";

        var roleClaimType =
            oidc["RoleClaimType"] ?? "roles";

        // --------------------------------------------------
        // 2. AUTENTICACIÓN JWT BEARER
        // --------------------------------------------------

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Servidor que emite los tokens.
                // Por ejemplo, un proveedor OIDC de ARCA.
                options.Authority = authority;

                // Identificador de nuestra API.
                // El token debe estar destinado a Facturación.
                options.Audience = audience;

                // La metadata del proveedor debe obtenerse
                // mediante HTTPS.
                options.RequireHttpsMetadata = true;

                // Conservamos los nombres originales de
                // los claims que llegan en el token.
                options.MapInboundClaims = false;

                // --------------------------------------------------
                // 3. VALIDACIÓN DEL TOKEN
                // --------------------------------------------------

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        // Comprueba quién emitió el token.
                        ValidateIssuer = true,

                        // Comprueba que el token esté destinado
                        // a esta API.
                        ValidateAudience = true,

                        // Comprueba que el token no esté vencido.
                        ValidateLifetime = true,

                        // Comprueba la firma del token.
                        ValidateIssuerSigningKey = true,

                        // No agregamos tolerancia extra al
                        // vencimiento del token.
                        ClockSkew = TimeSpan.Zero,

                        // Claim utilizado como nombre.
                        NameClaimType = nameClaimType,

                        // Claim utilizado para los roles.
                        RoleClaimType = roleClaimType
                    };

                // --------------------------------------------------
                // 4. EVENTOS DE AUTENTICACIÓN
                // --------------------------------------------------

                options.Events = new JwtBearerEvents
                {
                    // Se ejecuta después de validar correctamente
                    // la firma, el emisor, la audiencia y la vigencia.
                    OnTokenValidated = context =>
                    {
                        var usuario = context.Principal;

                        // Exigimos un identificador estable.
                        // No usamos el nombre como identificador.
                        var identificador =
                            usuario?.FindFirst("sub")?.Value;

                        if (string.IsNullOrWhiteSpace(identificador))
                        {
                            context.Fail(
                                "El token no contiene un identificador de usuario.");
                        }

                        return Task.CompletedTask;
                    },

                    // Cuando falla la autenticación,
                    // ASP.NET Core responde con 401.
                    OnChallenge = context =>
                    {
                        // Mantenemos el comportamiento estándar.
                        // No exponemos detalles internos del error.
                        return Task.CompletedTask;
                    },

                    // Cuando el usuario está autenticado pero
                    // no tiene permisos, se devuelve 403.
                    OnForbidden = context =>
                    {
                        return Task.CompletedTask;
                    }
                };
            });

        // --------------------------------------------------
        // 5. AUTORIZACIÓN
        // --------------------------------------------------

        services.AddAuthorizationBuilder()

            // Protegemos los endpoints por defecto.
            // Solamente [AllowAnonymous] permite acceso público.
            .SetFallbackPolicy(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())

            // ADMIN y OPERADOR pueden consultar.
            .AddPolicy(
                Politicas.Consultar,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.Admin, Roles.Operador))

            // Solo ADMIN puede gestionar el catálogo.
            .AddPolicy(
                Politicas.GestionarCatalogo,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.Admin))

            // Solo ADMIN puede administrar usuarios.
            .AddPolicy(
                Politicas.AdministrarUsuarios,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.Admin));

        return services;
    }
}