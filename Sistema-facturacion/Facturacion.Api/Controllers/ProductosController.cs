using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Services.Productos;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController : ControllerBase
{
    private readonly IProductoService service;

    public ProductosController(IProductoService service)
    {
        this.service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Producto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("activos")]
    public async Task<ActionResult<IReadOnlyList<Producto>>> GetActivos(CancellationToken cancellationToken)
        => Ok(await service.GetActivosAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("codigo/{codigo}")]
    public async Task<ActionResult<Producto>> GetByCodigo(string codigo, CancellationToken cancellationToken)
    {
        var producto = await service.GetByCodigoAsync(codigo, cancellationToken);
        return producto is null ? NotFound() : Ok(producto);
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> Create(Producto producto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateAsync(producto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Producto>> Update(
        int id,
        Producto producto,
        CancellationToken cancellationToken)
    {
        producto.Id = id;

        try
        {
            return Ok(await service.UpdateAsync(producto, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
