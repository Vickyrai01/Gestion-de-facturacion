using Facturacion.BuildingBlocks.Infrastructure.Web;
using Facturacion.BuildingBlocks.Seguridad;
using Facturacion.Modules.Clientes.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Modules.Clientes.Presentation;

public sealed record ClienteRequest(
    string? Nombre,
    string? Apellido,
    string? Documento,
    string? Email);

/// <summary>
/// Controller delgado del módulo Clientes.
///
/// Comparar con `legacy/legacyGestionDeFacturacion.web/Controllers/ClientesController.cs`
/// y con `Sistema-facturacion/Facturacion.Api/Controllers/ClientesController.cs`:
/// - No instancia ni conoce `DbContext`.
/// - No recibe ni devuelve entidades de persistencia (`Models.Cliente`).
/// - No tiene bloques try/catch por cada acción.
/// - Respeta la baja lógica (`Desactivar`) del legacy además de `DELETE`.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Policy = Politicas.Operar)]
public sealed class ClientesController(IServicioClientes servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await servicio.ListarAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> ObtenerPorId(int id, CancellationToken cancellationToken) =>
        (await servicio.ObtenerPorIdAsync(id, cancellationToken)).ToActionResult(this);

    [HttpGet("documento/{documento}")]
    public async Task<ActionResult<ClienteDto>> ObtenerPorDocumento(string documento, CancellationToken cancellationToken) =>
        (await servicio.ObtenerPorDocumentoAsync(documento, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<ClienteDto>> Crear(ClienteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(
            new GuardarClienteComando(request.Nombre, request.Apellido, request.Documento, request.Email),
            cancellationToken);

        return resultado.EsExito
            ? CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Valor.Id }, resultado.Valor)
            : this.ToProblemActionResult(resultado.Error);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult<ClienteDto>> Actualizar(int id, ClienteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarAsync(
            id,
            new GuardarClienteComando(request.Nombre, request.Apellido, request.Documento, request.Email),
            cancellationToken);

        return resultado.ToActionResult(this);
    }

    /// <summary>
    /// En el legacy `ClientesController.Desactivar` hacía baja lógica (`Activo = false`).
    /// Se expone tanto por PATCH/POST `/desactivar` como por DELETE para clientes REST.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [HttpPatch("{id:int}/desactivar")]
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Politicas.Administrar)]
    public async Task<ActionResult> Desactivar(int id, CancellationToken cancellationToken) =>
        (await servicio.DesactivarAsync(id, cancellationToken)).ToNoContentResult(this);
}

