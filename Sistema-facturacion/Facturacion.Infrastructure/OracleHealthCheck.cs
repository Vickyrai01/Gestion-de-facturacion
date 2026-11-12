using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Facturacion.Infrastructure;

public sealed class OracleHealthCheck : IHealthCheck
{
    private readonly FacturacionDbContext context;

    public OracleHealthCheck(FacturacionDbContext context)
    {
        this.context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isAvailable = await this.context.Database.CanConnectAsync(cancellationToken);
            return isAvailable
                ? HealthCheckResult.Healthy("La conexión con Oracle está disponible.")
                : HealthCheckResult.Unhealthy("No se puede conectar con Oracle.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("La comprobación de Oracle ha fallado.", exception);
        }
    }
}
