using System.Security.Claims;
using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Facturacion.Api.Seguridad;

/// <summary>
/// Arma la identidad (claims) que viaja en la cookie: id estable, nombre y rol.
/// Equivale al GenericPrincipal que el legacy armaba en Global.asax.
/// </summary>
public sealed class UsuarioClaimsPrincipalFactory : UserClaimsPrincipalFactory<Usuario>
{
    public UsuarioClaimsPrincipalFactory(
        UserManager<Usuario> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, user.Rol));
        return identity;
    }
}

/// <summary>
/// Un usuario inactivo no puede iniciar sesión. SignInManager devuelve
/// <see cref="Microsoft.AspNetCore.Identity.SignInResult.NotAllowed"/>.
/// </summary>
public sealed class UsuarioActivoConfirmation : IUserConfirmation<Usuario>
{
    public Task<bool> IsConfirmedAsync(UserManager<Usuario> manager, Usuario user)
        => Task.FromResult(user.Activo);
}
