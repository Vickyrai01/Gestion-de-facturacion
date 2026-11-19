using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Clientes.Application;
using Facturacion.Modules.Clientes.Contracts;
using Facturacion.Modules.Clientes.Domain;
using Facturacion.Modules.Clientes.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Modules.Clientes;

/// <summary>
/// Punto único de registro del módulo Clientes en el Host.
/// Cada módulo registra sus propios servicios, su DbContext y su contrato público.
/// </summary>
public static class ClientesModule
{
    public static IServiceCollection AddModuloClientes(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextModulo<ClientesDbContext>(configuration);
        services.AddScoped<IRepositorioClientes, RepositorioClientesEf>();
        services.AddScoped<IServicioClientes, ServicioClientes>();
        services.AddScoped<IConsultaClientes, ConsultaClientesModulo>();
        return services;
    }

    /// <summary>
    /// Carga los 5 clientes de demostración de `Configuration.Seed` del legacy
    /// (Ana García, Luis Pérez, Marta López [inactiva], Diego Ruiz, Sofía Torres).
    /// </summary>
    public static async Task SeedDatosDemoAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientesDbContext>();

        string[] nombres = ["Ana", "Luis", "Marta", "Diego", "Sofía"];
        string[] apellidos = ["García", "Pérez", "López", "Ruiz", "Torres"];
        string[] documentos = ["30123456", "28987654", "32555666", "33444555", "35666777"];

        for (var i = 0; i < nombres.Length; i++)
        {
            var documento = documentos[i];
            if (await db.Clientes.AnyAsync(c => c.Documento == documento, cancellationToken))
                continue;

            var cliente = Cliente.Crear(
                nombres[i],
                apellidos[i],
                documento,
                $"cliente{i + 1}@example.com").Valor;

            // En el legacy i == 2 (Marta López) se creaba inactiva para demostrar la validación al facturar.
            if (i == 2)
                cliente.Desactivar();

            await db.Clientes.AddAsync(cliente, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

