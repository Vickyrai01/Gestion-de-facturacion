using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Clientes.Contracts;
using Facturacion.Modules.Facturacion.Application;
using Facturacion.Modules.Facturacion.Domain;
using Facturacion.Modules.Facturacion.Infrastructure;
using Facturacion.Modules.Productos.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Modules.Facturacion;

public static class FacturacionModule
{
    public static IServiceCollection AddModuloFacturacion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpcionesFacturacion>(configuration.GetSection(OpcionesFacturacion.Seccion));
        services.AddDbContextModulo<FacturasDbContext>(configuration);
        services.AddScoped<IRepositorioFacturas, RepositorioFacturasEf>();
        services.AddScoped<IServicioFacturacion, ServicioFacturacion>();
        services.AddHostedService<ResumenPendientesWorker>();
        return services;
    }

    /// <summary>
    /// Carga las 3 facturas de demostración (`DEMO-001` PENDIENTE, `DEMO-002` EMITIDA, `DEMO-003` ANULADA)
    /// exactamente como `Configuration.Seed` del legacy.
    /// </summary>
    public static async Task SeedDatosDemoAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FacturasDbContext>();
        var consultaClientes = scope.ServiceProvider.GetRequiredService<IConsultaClientes>();
        var consultaProductos = scope.ServiceProvider.GetRequiredService<IConsultaProductos>();

        var clientes = await consultaClientes.ListarActivosAsync(cancellationToken);
        var productos = (await consultaProductos.ListarActivosAsync(cancellationToken)).OrderBy(p => p.Id).Take(2).ToList();

        if (clientes.Count == 0 || productos.Count < 2)
            return;

        var clienteInicial = clientes.OrderBy(c => c.Id).First();
        var precios = productos.ToDictionary(
            p => p.Id,
            p => new PrecioProductoVigente(p.Id, p.PrecioUnitario));
        var lineas = productos.Select(p => new LineaFacturaSolicitud(p.Id, 2)).ToList();

        for (var i = 0; i < EstadosFactura.Todos.Count; i++)
        {
            var numero = $"DEMO-{i + 1:D3}";
            if (await db.Facturas.AnyAsync(f => f.Numero == numero, cancellationToken))
                continue;

            var factura = Factura.Crear(
                numero,
                clienteInicial.Id,
                DateTime.UtcNow.Date.AddDays(-i),
                lineas,
                precios,
                0.21m).Valor;

            if (EstadosFactura.Todos[i] == EstadosFactura.Emitida)
                factura.Emitir();
            else if (EstadosFactura.Todos[i] == EstadosFactura.Anulada)
                factura.Anular();

            await db.Facturas.AddAsync(factura, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

