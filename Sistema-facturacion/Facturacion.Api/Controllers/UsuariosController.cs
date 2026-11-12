using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Services.Usuarios;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioService service;

    public UsuariosController(IUsuarioService service)
    {
        this.service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Usuario>>> GetAll(CancellationToken cancellationToken)
        => Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Usuario>> GetById(int id, CancellationToken cancellationToken)
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

    [HttpGet("nombre/{nombreusuario}")]
    public async Task<ActionResult<Usuario>> GetByNombreusuario(
        string nombreusuario,
        CancellationToken cancellationToken)
    {
        var usuario = await service.GetByNombreusuarioAsync(nombreusuario, cancellationToken);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create(Usuario usuario, CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateAsync(usuario, cancellationToken);
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
    public async Task<ActionResult<Usuario>> Update(
        int id,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        usuario.Id = id;

        try
        {
            return Ok(await service.UpdateAsync(usuario, cancellationToken));
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
