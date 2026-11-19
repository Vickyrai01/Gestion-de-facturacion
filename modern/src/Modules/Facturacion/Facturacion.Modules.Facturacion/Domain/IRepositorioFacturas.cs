namespace Facturacion.Modules.Facturacion.Domain;

/// <summary>
/// Proyección de resumen por estado para el reporte administrativo (`FacturasController.Reportes` del legacy).
/// </summary>
public sealed record ResumenEstadoFacturas(string Estado, int Cantidad, decimal Total);

/// <summary>
/// Puerto de persistencia del agregado <see cref="Factura"/>.
/// </summary>
public interface IRepositorioFacturas
{
    /// <summary>
    /// Ejecuta la creación dentro de una transacción aislada, asignando el siguiente número correlativo.
    /// </summary>
    Task<Factura> CrearConNumeroCorrelativoAsync(
        Func<string, CancellationToken, Task<Factura>> construirFactura,
        CancellationToken cancellationToken = default);

    Task<Factura?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Factura>> ConsultarAsync(
        int? clienteId = null,
        string? estado = null,
        CancellationToken cancellationToken = default);

    Task<int> ContarPendientesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResumenEstadoFacturas>> ObtenerResumenPorEstadoAsync(
        CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

