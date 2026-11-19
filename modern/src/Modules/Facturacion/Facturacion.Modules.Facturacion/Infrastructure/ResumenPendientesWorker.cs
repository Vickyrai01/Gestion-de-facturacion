using Facturacion.Modules.Facturacion.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Facturacion.Modules.Facturacion.Infrastructure;

/// <summary>
/// Worker periódicamente ejecutado en segundo plano.
///
/// Evolución respecto del legacy y de la Clase 5:
/// - En `legacy/FacturacionLegacy.WindowsService/ResumenPendientes.cs` era un Windows Service con `System.Threading.Timer`
///   que abría directamente `new FacturacionDbContext()` y llamaba `FacturacionOperaciones.ContarPendientes(db)`.
/// - En la Clase 5 (`Sistema-facturacion/Facturacion.Api/FacturacionWorker.cs`) sólo escribía un timestamp sin consultar las facturas.
/// - En la Clase 7 vive dentro del módulo Facturación y consume el caso de uso `IServicioFacturacion.ContarPendientesAsync`,
///   respetando la separación de capas.
/// </summary>
internal sealed class ResumenPendientesWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OpcionesFacturacion> opciones,
    ILogger<ResumenPendientesWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var segundos = Math.Clamp(opciones.Value.IntervaloResumenSegundos, 5, 86400);
        var intervalo = TimeSpan.FromSeconds(segundos);

        logger.LogInformation(
            "ResumenPendientesWorker iniciado dentro del módulo Facturación. Intervalo: {IntervaloSegundos}s",
            segundos);

        await RegistrarResumenUnaVezAsync(stoppingToken);

        using var timer = new PeriodicTimer(intervalo);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RegistrarResumenUnaVezAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ResumenPendientesWorker detenido.");
        }
    }

    internal async Task<int> RegistrarResumenUnaVezAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IServicioFacturacion>();
            var pendientes = await servicio.ContarPendientesAsync(cancellationToken);

            logger.LogInformation("Facturas pendientes: {CantidadPendientes}", pendientes);
            return pendientes;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Error de procesamiento en ResumenPendientesWorker: {TipoError}", ex.GetType().Name);
            return -1;
        }
    }
}

