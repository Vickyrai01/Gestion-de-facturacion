namespace Facturacion.Api;

public sealed class FacturacionWorker(
    ILogger<FacturacionWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        Math.Max(1, configuration.GetValue<int?>("Worker:IntervalSeconds") ?? 20));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FacturacionWorker iniciado. Intervalo: {Interval}", _interval);

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogInformation("FacturacionWorker ejecutó su ciclo periódico en {Timestamp}", DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("FacturacionWorker detenido");
        }
    }
}
