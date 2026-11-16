using System.ComponentModel.DataAnnotations;
using Facturacion.Api.Seguridad;
using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Controllers;

/// <summary>Lo que la API muestra de un usuario: nunca el hash.</summary>
public sealed record UsuarioResponse(
    int Id,
    string NombreUsuario,
    string Rol,
    bool Activo,
    string FormatoHash)
{
    public static UsuarioResponse Desde(Usuario usuario) => new(
        usuario.Id,
        usuario.Nombreusuario,
        usuario.Rol,
        usuario.Activo,
        PasswordHasherCompatibleLegacy.EsHashLegacy(usuario.Passwordhash)
            ? "BCrypt (legacy, pendiente de migrar)"
            : "Identity");
}

public sealed record CrearUsuarioRequest(
    [Required, MaxLength(50)] string NombreUsuario,
    [Required] string Password,
    [Required] string Rol);

public sealed record ActualizarUsuarioRequest(
    [Required] string Rol,
    bool Activo);

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Politicas.AdministrarUsuarios)]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioService service;
    private readonly UserManager<Usuario> userManager;

    public UsuariosController(IUsuarioService service, UserManager<Usuario> userManager)
    {
        this.service = service;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok((await service.GetAllAsync(cancellationToken)).Select(UsuarioResponse.Desde).ToList());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> GetById(int id)
    {
        var usuario = await userManager.FindByIdAsync(id.ToString());
        return usuario is null ? NotFound() : Ok(UsuarioResponse.Desde(usuario));
    }

    [HttpGet("nombre/{nombreusuario}")]
    public async Task<ActionResult<UsuarioResponse>> GetByNombreusuario(string nombreusuario)
    {
        var usuario = await userManager.FindByNameAsync(nombreusuario);
        return usuario is null ? NotFound() : Ok(UsuarioResponse.Desde(usuario));
    }

    /// <summary>
    /// Alta de usuario. Recibe la contraseña (no un hash): Identity la valida
    /// y la guarda con su propio formato.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Create(CrearUsuarioRequest request)
    {
        if (!Roles.EsValido(request.Rol))
            return BadRequest(new ProblemDetails { Detail = $"Rol inválido. Valores posibles: {string.Join(", ", Roles.Todos)}." });

        var usuario = new Usuario
        {
            Nombreusuario = request.NombreUsuario.Trim(),
            Passwordhash = "",
            Rol = request.Rol.Trim().ToUpperInvariant(),
            Activo = true
        };

        var resultado = await userManager.CreateAsync(usuario, request.Password);
        if (!resultado.Succeeded)
            return ErroresDeIdentity(resultado);

        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, UsuarioResponse.Desde(usuario));
    }

    /// <summary>Cambia rol y estado. La cookie del usuario afectado se revalida en su próxima petición.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> Update(int id, ActualizarUsuarioRequest request)
    {
        if (!Roles.EsValido(request.Rol))
            return BadRequest(new ProblemDetails { Detail = $"Rol inválido. Valores posibles: {string.Join(", ", Roles.Todos)}." });

        var usuario = await userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
            return NotFound();

        usuario.Rol = request.Rol.Trim().ToUpperInvariant();
        usuario.Activo = request.Activo;

        var resultado = await userManager.UpdateAsync(usuario);
        return resultado.Succeeded ? Ok(UsuarioResponse.Desde(usuario)) : ErroresDeIdentity(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
            return NotFound();

        var resultado = await userManager.DeleteAsync(usuario);
        return resultado.Succeeded ? NoContent() : ErroresDeIdentity(resultado);
    }

    private ActionResult ErroresDeIdentity(IdentityResult resultado)
    {
        foreach (var error in resultado.Errors)
            ModelState.AddModelError(error.Code, error.Description);

        return resultado.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateUserName))
            ? Conflict(new ValidationProblemDetails(ModelState))
            : ValidationProblem(ModelState);
    }
}
