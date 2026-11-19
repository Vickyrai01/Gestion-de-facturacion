using System.Security.Claims;
using Facturacion.BuildingBlocks.Infrastructure.Web;
using Facturacion.BuildingBlocks.Seguridad;
using Facturacion.Modules.Usuarios.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Modules.Usuarios.Presentation;

public sealed record LoginRequest(string? NombreUsuario, string? Password);

public sealed record IdentidadActualResponse(
    string Id,
    string? NombreUsuario,
    IReadOnlyList<string> Roles);

public sealed record CrearUsuarioRequest(
    string? NombreUsuario,
    string? Password,
    string? Rol);

public sealed record ActualizarUsuarioRequest(
    string? Rol,
    string? NuevoPassword,
    bool? Activo);

/// <summary>
/// Controller de autenticación (`POST /api/auth/login`, `GET /api/auth/yo`).
/// Reemplaza a `AccountController` del legacy sin lógica de base de datos ni de ventanas de bloqueo en el controller.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IServicioUsuarios servicio) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultadoDto>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        (await servicio.LoginAsync(new LoginComando(request.NombreUsuario, request.Password), cancellationToken))
            .ToActionResult(this);

    [HttpGet("yo")]
    [Authorize]
    public ActionResult<IdentidadActualResponse> Yo()
    {
        var id = User.FindFirst("sub")?.Value
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        return Ok(new IdentidadActualResponse(id, User.Identity?.Name, roles));
    }
}

/// <summary>
/// Controller de administración de usuarios (`api/usuarios`).
///
/// Correcciones clave frente a `Sistema-facturacion/Facturacion.Api/Controllers/UsuariosController.cs`:
/// 1. Está protegido con `[Authorize(Policy = Politicas.Administrar)]`.
/// 2. Devuelve `UsuarioDto` (sin `PasswordHash`) en lugar de la entidad de persistencia `Usuario`.
/// 3. No contiene bloques try/catch ni lógica de validación.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Politicas.Administrar)]
public sealed class UsuariosController(IServicioUsuarios servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await servicio.ListarAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> ObtenerPorId(int id, CancellationToken cancellationToken) =>
        (await servicio.ObtenerPorIdAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(
            new CrearUsuarioComando(request.NombreUsuario, request.Password, request.Rol),
            cancellationToken);

        return resultado.EsExito
            ? CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Valor.Id }, resultado.Valor)
            : this.ToProblemActionResult(resultado.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Actualizar(
        int id,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken) =>
        (await servicio.ActualizarAsync(
            id,
            new ActualizarUsuarioComando(request.Rol, request.NuevoPassword, request.Activo),
            cancellationToken)).ToActionResult(this);

    [HttpPost("{id:int}/desactivar")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Desactivar(int id, CancellationToken cancellationToken) =>
        (await servicio.DesactivarAsync(id, cancellationToken)).ToNoContentResult(this);
}

