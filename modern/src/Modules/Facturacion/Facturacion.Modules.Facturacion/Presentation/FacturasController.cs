using Facturacion.BuildingBlocks.Infrastructure.Web;
using Facturacion.BuildingBlocks.Seguridad;
using Facturacion.Modules.Facturacion.Application;
using Facturacion.Modules.Facturacion.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Modules.Facturacion.Presentation;

public sealed record LineaFacturaRequest(int ProductoId, int Cantidad);

public sealed record CrearFacturaRequest(
    int ClienteId,
    IReadOnlyList<LineaFacturaRequest>? Detalles,
    decimal? TasaImpuestos = null);

/// <summary>
/// Controller delgado del módulo Facturación.
///
/// Comparar con `legacy/legacyGestionDeFacturacion.web/Controllers/FacturasController.cs`
/// y con `legacy/FacturacionLegacy.Wcf/FacturaConsultaService.svc.cs`:
/// - En el legacy `FacturasController` tenía `private readonly FacturacionDbContext db = new FacturacionDbContext();`,
///   leía `ConfigurationManager.AppSettings["TasaImpuestos"]`, armaba `GroupBy` sobre `db.Facturas`,
///   consultaba `db.Clientes` y `db.Productos` directamente y capturaba `ArgumentException`.
/// - Además reemplaza las tres operaciones de consulta de WCF (`ObtenerFacturaPorId`,
///   `ObtenerFacturasPorCliente`, `ObtenerFacturasPorEstado`).
/// - Acá cada acción tiene 1 a 4 líneas y sólo delega en `IServicioFacturacion`.
/// </summary>
[ApiController]
[Route("api/facturas")]
[Authorize(Policy = Politicas.Operar)]
public sealed class FacturasController(IServicioFacturacion servicio) : ControllerBase
{
    /// <summary>
    /// Consulta facturas con filtro opcional por cliente y/o estado (`PENDIENTE`, `EMITIDA`, `ANULADA`).
    /// Cubre tanto `FacturasController.Index` como `ObtenerFacturasPorCliente` y `ObtenerFacturasPorEstado` del WCF legacy.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FacturaResumenDto>>> Consultar(
        [FromQuery] int? clienteId,
        [FromQuery] string? estado,
        CancellationToken cancellationToken) =>
        (await servicio.ConsultarAsync(clienteId, estado, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Obtiene una factura con el detalle de sus renglones.
    /// Cubre tanto `FacturasController.Details` como `ObtenerFacturaPorId` del WCF legacy.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FacturaDetalleDto>> ObtenerPorId(
        int id,
        CancellationToken cancellationToken) =>
        (await servicio.ObtenerPorIdAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Devuelve los catálogos activos (clientes, productos y tasa configurada) para armar una nueva factura.
    /// Reemplaza a `FacturasController.PrepararFormulario` del legacy.
    /// </summary>
    [HttpGet("formulario")]
    public async Task<ActionResult<CatalogoFacturacionDto>> ObtenerDatosFormulario(
        CancellationToken cancellationToken) =>
        Ok(await servicio.ObtenerCatalogoFormularioAsync(cancellationToken));

    /// <summary>
    /// Crea una factura en estado PENDIENTE verificando cliente/productos activos y calculando totales.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FacturaDetalleDto>> Crear(
        CrearFacturaRequest request,
        CancellationToken cancellationToken)
    {
        var lineas = request.Detalles?
            .Select(d => new LineaFacturaSolicitud(d.ProductoId, d.Cantidad))
            .ToList();

        var resultado = await servicio.CrearAsync(
            new CrearFacturaComando(request.ClienteId, lineas, request.TasaImpuestos),
            cancellationToken);

        return resultado.EsExito
            ? CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Valor.Id }, resultado.Valor)
            : this.ToProblemActionResult(resultado.Error);
    }

    [HttpPost("{id:int}/emitir")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<FacturaDetalleDto>> Emitir(
        int id,
        CancellationToken cancellationToken) =>
        (await servicio.EmitirAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:int}/anular")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<FacturaDetalleDto>> Anular(
        int id,
        CancellationToken cancellationToken) =>
        (await servicio.AnularAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Reporte agrupado por estado (`FacturasController.Reportes` del legacy).
    /// </summary>
    [HttpGet("reportes")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<IReadOnlyList<ResumenEstadoFacturas>>> Reportes(
        CancellationToken cancellationToken) =>
        Ok(await servicio.ReportePorEstadoAsync(cancellationToken));

    /// <summary>
    /// Cantidad de facturas pendientes (misma consulta que registra el worker de segundo plano).
    /// </summary>
    [HttpGet("pendientes")]
    public async Task<ActionResult<object>> ContarPendientes(CancellationToken cancellationToken) =>
        Ok(new { cantidad = await servicio.ContarPendientesAsync(cancellationToken) });
}

