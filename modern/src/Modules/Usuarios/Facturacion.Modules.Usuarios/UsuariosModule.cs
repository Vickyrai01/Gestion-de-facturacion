using System.Security.Claims;
using System.Text;
using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.BuildingBlocks.Seguridad;
using Facturacion.Modules.Usuarios.Application;
using Facturacion.Modules.Usuarios.Domain;
using Facturacion.Modules.Usuarios.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Facturacion.Modules.Usuarios;

public static class UsuariosModule
{
    public const string PasswordDemoAdmin = "FACTURACION_ADMIN_PASSWORD";
    public const string PasswordDemoOperador = "FACTURACION_OPERADOR_PASSWORD";

    public static IServiceCollection AddModuloUsuarios(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpcionesJwt>(configuration.GetSection(OpcionesJwt.Seccion));
        var jwt = configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();

        services.AddDbContextModulo<UsuariosDbContext>(configuration);
        services.AddScoped<IRepositorioUsuarios, RepositorioUsuariosEf>();
        services.AddSingleton<IHasherPassword>(_ => new HasherPasswordBcrypt(workFactor: 10));
        services.AddSingleton<IControlIntentosLogin, ControlIntentosLoginEnMemoria>();
        services.AddSingleton<IEmisorToken, EmisorJwtSimetrico>();
        services.AddScoped<IServicioUsuarios, ServicioUsuarios>();

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ClaveSimetrica));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = clave,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Politicas.Operar, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Admin, Roles.Operador))
            .AddPolicy(Politicas.Administrar, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Admin));

        return services;
    }

    /// <summary>
    /// Prepara las cuentas locales de demostración (`admin` y `operador`) igual que `FacturacionLegacy.Tools usuarios-demo`.
    /// </summary>
    public static async Task SeedDatosDemoAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IHasherPassword>();

        var adminPassword = Environment.GetEnvironmentVariable("FACTURACION_ADMIN_PASSWORD") ?? PasswordDemoAdmin;
        var operadorPassword = Environment.GetEnvironmentVariable("FACTURACION_OPERADOR_PASSWORD") ?? PasswordDemoOperador;

        if (!await db.Usuarios.AnyAsync(u => u.NombreUsuario == "admin", cancellationToken))
        {
            var admin = Usuario.Crear("admin", hasher.CrearHash(adminPassword).Valor, Roles.Admin).Valor;
            await db.Usuarios.AddAsync(admin, cancellationToken);
        }

        if (!await db.Usuarios.AnyAsync(u => u.NombreUsuario == "operador", cancellationToken))
        {
            var operador = Usuario.Crear("operador", hasher.CrearHash(operadorPassword).Valor, Roles.Operador).Valor;
            await db.Usuarios.AddAsync(operador, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

