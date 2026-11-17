using System.Threading.RateLimiting;
using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace Facturacion.Api.Seguridad;

public static class SeguridadExtensions
{
    /// <summary>Esquema "selector": elige cookie o bearer según la petición.</summary>
    public const string EsquemaCookieOBearer = "CookieOBearer";

    public const string PoliticaRateLimitLogin = "login";

    /// <summary>
    /// Registra Identity (sobre la tabla USUARIOS), la cookie de autenticación,
    /// la validación opcional de access tokens JWT emitidos por un proveedor
    /// OIDC y las políticas de autorización.
    /// </summary>
    public static IServiceCollection AddSeguridad(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- Identity: usuarios, contraseñas y roles ------------------------
        services.AddScoped<PasswordHasher<Usuario>>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasherCompatibleLegacy>();
        services.AddScoped<IUserConfirmation<Usuario>, UsuarioActivoConfirmation>();

        services
            .AddIdentityCore<Usuario>(options =>
            {
                // Misma regla que el legacy: al menos 12 caracteres.
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // "Cuenta confirmada" = USUARIOS.ACTIVO = 1 (ver UsuarioActivoConfirmation).
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddUserStore<UsuarioStore>()
            .AddClaimsPrincipalFactory<UsuarioClaimsPrincipalFactory>()
            .AddSignInManager();

        // ---- Autenticación: cookie (navegador) o bearer (clientes de API) ---
        var oidc = configuration.GetSection("Autenticacion:Oidc");
        var authority = oidc["Authority"];
        var bearerHabilitado = !string.IsNullOrWhiteSpace(authority);

        var authentication = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = EsquemaCookieOBearer;
                options.DefaultChallengeScheme = EsquemaCookieOBearer;
            })
            .AddPolicyScheme(EsquemaCookieOBearer, "Cookie o Bearer", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string? authorization = context.Request.Headers.Authorization;
                    return bearerHabilitado
                           && authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                        ? JwtBearerDefaults.AuthenticationScheme
                        : IdentityConstants.ApplicationScheme;
                };
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = "Facturacion.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                // Strict: el navegador no envía la cookie en peticiones iniciadas
                // desde otro sitio. Es la defensa CSRF de esta API (no hay vistas
                // con antiforgery como en el MVC legacy).
                options.Cookie.SameSite = SameSiteMode.Strict;
                // Igual que el legacy: 20 minutos, sin renovación deslizante.
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                options.SlidingExpiration = false;

                options.Events = new CookieAuthenticationEvents
                {
                    // Una API no redirige a una página de login: responde 401/403.
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = RevalidarUsuarioAsync
                };
            });

        if (bearerHabilitado)
        {
            // La API no emite tokens: los emite el proveedor (Keycloak, Entra ID,
            // Auth0, ...). Acá solo se validan firma, emisor, audiencia y vencimiento.
            authentication.AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = oidc["Audience"];
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = oidc["NameClaimType"] ?? "preferred_username";
                options.TokenValidationParameters.RoleClaimType = oidc["RoleClaimType"] ?? "roles";
            });
        }

        // ---- Autorización ---------------------------------------------------
        services.AddAuthorizationBuilder()
            // Todo endpoint sin [Authorize]/[AllowAnonymous] explícito exige
            // un usuario autenticado: protegido por defecto.
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Politicas.Consultar, policy => policy.RequireRole(Roles.Admin, Roles.Operador))
            .AddPolicy(Politicas.GestionarCatalogo, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Politicas.AdministrarUsuarios, policy => policy.RequireRole(Roles.Admin));

        // ---- Límite de intentos de login ------------------------------------
        // Reemplaza a IntentosLogin.cs del legacy. Es por IP y en memoria: igual
        // que antes, no se comparte entre varios servidores.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PoliticaRateLimitLogin, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5)
                    }));
        });

        return services;
    }

    /// <summary>
    /// En cada petición con cookie se vuelve a leer la cuenta, como hacía el
    /// legacy en Application_PostAuthenticateRequest: si se desactivó, la cookie
    /// deja de valer; si cambió el rol, se reemite la cookie con el rol nuevo.
    /// </summary>
    private static async Task RevalidarUsuarioAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is null)
            return;

        var services = context.HttpContext.RequestServices;
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var usuario = await userManager.GetUserAsync(context.Principal);

        if (usuario is null || !usuario.Activo)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        if (!context.Principal.IsInRole(usuario.Rol))
        {
            var signInManager = services.GetRequiredService<SignInManager<Usuario>>();
            context.ReplacePrincipal(await signInManager.CreateUserPrincipalAsync(usuario));
            context.ShouldRenew = true;
        }
    }
}
