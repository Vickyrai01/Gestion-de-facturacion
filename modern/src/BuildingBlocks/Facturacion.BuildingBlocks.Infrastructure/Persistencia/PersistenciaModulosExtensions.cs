using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Facturacion.BuildingBlocks.Aplicacion;

namespace Facturacion.BuildingBlocks.Infrastructure.Persistencia;

/// <summary>
/// Opciones compartidas de base de datos para todos los DbContext modulares.
/// En un monolito modular todos los módulos comparten el mismo motor (SQL Server / LocalDB
/// heredado del legacy, o InMemory para tests rápidos y demos sin SQL), pero cada módulo
/// tiene su propio DbContext acotado a sus propias tablas.
/// </summary>
public static class PersistenciaModulosExtensions
{
    public const string ProveedorInMemory = "InMemory";
    public const string ProveedorSqlServer = "SqlServer";

    public static IServiceCollection AddDbContextModulo<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        var proveedor = configuration["Persistencia:Proveedor"] ?? ProveedorInMemory;
        var connectionString = configuration.GetConnectionString("FacturacionDb");
        var nombreInMemory = configuration["Persistencia:NombreInMemory"] ?? "FacturacionModularDb";

        services.AddDbContext<TContext>(options =>
        {
            if (string.Equals(proveedor, ProveedorSqlServer, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Se configuró Persistencia:Proveedor=SqlServer pero falta ConnectionStrings:FacturacionDb.");
                }

                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseInMemoryDatabase(nombreInMemory);
            }
        });

        return services;
    }

    public static bool EsViolacionIndiceUnico(DbUpdateException exception)
    {
        if (exception.GetBaseException() is SqlException sqlException)
            return sqlException.Number is 2601 or 2627;

        return false;
    }

    public static async Task GuardarManejandoConflictosAsync(
        DbContext db,
        string mensajeSiDuplicado,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsViolacionIndiceUnico(ex))
        {
            throw new ConflictoDePersistenciaException(mensajeSiDuplicado, ex);
        }
    }
}

