using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Productos.Application;
using Facturacion.Modules.Productos.Contracts;
using Facturacion.Modules.Productos.Domain;
using Facturacion.Modules.Productos.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Modules.Productos;

public static class ProductosModule
{
    public static IServiceCollection AddModuloProductos(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextModulo<ProductosDbContext>(configuration);
        services.AddScoped<IRepositorioProductos, RepositorioProductosEf>();
        services.AddScoped<IServicioProductos, ServicioProductos>();
        services.AddScoped<IConsultaProductos, ConsultaProductosModulo>();
        return services;
    }

    /// <summary>
    /// Carga los 8 productos de demostración de `Configuration.Seed` del legacy (`PROD-001`..`PROD-008`).
    /// </summary>
    public static async Task SeedDatosDemoAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductosDbContext>();

        for (var i = 1; i <= 8; i++)
        {
            var codigo = $"PROD-{i:D3}";
            if (await db.Productos.AnyAsync(p => p.Codigo == codigo, cancellationToken))
                continue;

            var producto = Producto.Crear(
                codigo,
                $"Producto de ejemplo {i}",
                "Material didáctico",
                i * 100m,
                i * 10).Valor;

            await db.Productos.AddAsync(producto, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

