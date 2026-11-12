using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Facturacion")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'Facturacion'.");

        services.AddDbContext<FacturacionDbContext>(options =>
            options.UseOracle(connectionString));
        services.AddHealthChecks()
            .AddCheck<OracleHealthCheck>("oracle");

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IFacturaRepository, FacturaRepository>();
        services.AddScoped<IDetallesfacturaRepository, DetallesfacturaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
