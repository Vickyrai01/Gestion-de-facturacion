using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Services.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController : ControllerBase
{
    private readonly IClienteService service;

    public ClientesController(IClienteService service)
    {
        this.service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Cliente>>> GetAll(CancellationToken cancellationToken)
        => Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cliente>> GetById(int id, CancellationToken cancellationToken)
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

    [HttpGet("documento/{documento}")]
    public async Task<ActionResult<Cliente>> GetByDocumento(string documento, CancellationToken cancellationToken)
    {
        var cliente = await service.GetByDocumentoAsync(documento, cancellationToken);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Create(Cliente cliente, CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateAsync(cliente, cancellationToken);
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
    public async Task<ActionResult<Cliente>> Update(
        int id,
        Cliente cliente,
        CancellationToken cancellationToken)
    {
        cliente.Id = id;

        try
        {
            return Ok(await service.UpdateAsync(cliente, cancellationToken));
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
