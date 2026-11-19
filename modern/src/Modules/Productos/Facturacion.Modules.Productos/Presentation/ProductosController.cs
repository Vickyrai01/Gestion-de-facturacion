using Facturacion.BuildingBlocks.Infrastructure.Web;
using Facturacion.BuildingBlocks.Seguridad;
using Facturacion.Modules.Productos.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Modules.Productos.Presentation;

public sealed record ProductoRequest(
    string? Codigo,
    string? Nombre,
    string? Descripcion,
    decimal PrecioUnitario,
    int Stock);

/// <summary>
/// Controller delgado del módulo Productos.
///
/// Comparar con `legacy/legacyGestionDeFacturacion.web/Controllers/ProductosController.cs`:
/// - En el legacy el controller tenía `ValidarCodigo`, `decimal.Round` y `catch (DbUpdateException)` con `SqlException` 2601/2627.
/// - En Clase 5 (`Sistema-facturacion/Facturacion.Api/Controllers/ProductosController.cs`) faltaban `[Authorize]` y usaba `Models.Producto`.
/// - Acá el controller sólo delega en `IServicioProductos` y aplica las políticas `Operar` y `Administrar`.
/// </summary>
[ApiController]
[Route("api/productos")]
[Authorize(Policy = Politicas.Operar)]
public sealed class ProductosController(IServicioProductos servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductoDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await servicio.ListarAsync(cancellationToken));

    [HttpGet("activos")]
    public async Task<ActionResult<IReadOnlyList<ProductoDto>>> ListarActivos(CancellationToken cancellationToken) =>
        Ok(await servicio.ListarActivosAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoDto>> ObtenerPorId(int id, CancellationToken cancellationToken) =>
        (await servicio.ObtenerPorIdAsync(id, cancellationToken)).ToActionResult(this);

    [HttpGet("codigo/{codigo}")]
    public async Task<ActionResult<ProductoDto>> ObtenerPorCodigo(string codigo, CancellationToken cancellationToken) =>
        (await servicio.ObtenerPorCodigoAsync(codigo, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<ProductoDto>> Crear(ProductoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(
            new GuardarProductoComando(
                request.Codigo,
                request.Nombre,
                request.Descripcion,
                request.PrecioUnitario,
                request.Stock),
            cancellationToken);

        return resultado.EsExito
            ? CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Valor.Id }, resultado.Valor)
            : this.ToProblemActionResult(resultado.Error);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<ProductoDto>> Actualizar(int id, ProductoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarAsync(
            id,
            new GuardarProductoComando(
                request.Codigo,
                request.Nombre,
                request.Descripcion,
                request.PrecioUnitario,
                request.Stock),
            cancellationToken);

        return resultado.ToActionResult(this);
    }

    [HttpPost("{id:int}/desactivar")]
    [HttpPatch("{id:int}/desactivar")]
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult> Desactivar(int id, CancellationToken cancellationToken) =>
        (await servicio.DesactivarAsync(id, cancellationToken)).ToNoContentResult(this);
}

