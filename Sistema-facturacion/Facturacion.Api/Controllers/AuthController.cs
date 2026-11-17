using System.ComponentModel.DataAnnotations;
using Facturacion.Api.Seguridad;
using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Facturacion.Api.Controllers;

public sealed record LoginRequest(
    [Required, MaxLength(50)] string NombreUsuario,
    [Required] string Password);

public sealed record UsuarioActualResponse(
    string Id,
    string? NombreUsuario,
    IReadOnlyList<string> Roles,
    string EsquemaAutenticacion);

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<Usuario> userManager;
    private readonly SignInManager<Usuario> signInManager;
    private readonly ILogger<AuthController> logger;

    public AuthController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        ILogger<AuthController> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    /// <summary>
    /// Verifica usuario y contraseña y emite la cookie de autenticación.
    /// Si la cuenta todavía tenía un hash BCrypt del legacy, Identity lo
    /// reemplaza por uno nuevo en este mismo paso.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SeguridadExtensions.PoliticaRateLimitLogin)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await userManager.FindByNameAsync(request.NombreUsuario);
        if (usuario is null)
        {
            // Igual que el legacy: se gasta el tiempo de una verificación para no
            // revelar, por el tiempo de respuesta, qué usuarios existen.
            userManager.PasswordHasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio, request.Password);
            return CredencialesInvalidas();
        }

        var teniaHashLegacy = PasswordHasherCompatibleLegacy.EsHashLegacy(usuario.Passwordhash);

        var resultado = await signInManager.PasswordSignInAsync(
            usuario,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!resultado.Succeeded)
        {
            // NotAllowed = cuenta inactiva. Se informa con el mismo mensaje genérico.
            logger.LogInformation(
                "Login rechazado para el usuario {UsuarioId}. NotAllowed={NotAllowed}",
                usuario.Id,
                resultado.IsNotAllowed);
            return CredencialesInvalidas();
        }

        if (teniaHashLegacy && !PasswordHasherCompatibleLegacy.EsHashLegacy(usuario.Passwordhash))
            logger.LogInformation("Cuenta {UsuarioId} migrada de BCrypt al hash de Identity.", usuario.Id);

        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Borra la cookie en el navegador. Una copia robada de la cookie sigue
        // siendo válida hasta vencer, salvo que la cuenta se desactive.
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return NoContent();
    }

    /// <summary>Muestra la identidad con la que llegó la petición (cookie o bearer).</summary>
    [HttpGet("yo")]
    [Authorize]
    public ActionResult<UsuarioActualResponse> Yo()
    {
        var identity = User.Identity!;
        var roleClaimType = (identity as System.Security.Claims.ClaimsIdentity)?.RoleClaimType
                            ?? System.Security.Claims.ClaimTypes.Role;

        return new UsuarioActualResponse(
            Id: userManager.GetUserId(User) ?? User.FindFirst("sub")?.Value ?? "",
            NombreUsuario: identity.Name,
            Roles: User.FindAll(roleClaimType).Select(claim => claim.Value).ToList(),
            EsquemaAutenticacion: identity.AuthenticationType ?? "");
    }

    private ObjectResult CredencialesInvalidas()
        => Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Usuario o contraseña incorrectos.");

    private static readonly Usuario UsuarioFicticio = new()
    {
        Nombreusuario = "ficticio",
        Passwordhash = "",
        Rol = Roles.Operador
    };

    private static readonly string HashFicticio =
        new PasswordHasher<Usuario>().HashPassword(UsuarioFicticio, Guid.NewGuid().ToString());
}
