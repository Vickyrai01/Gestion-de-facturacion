using System.Security.Claims;
using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Facturacion.Api.Seguridad;

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

public sealed class UsuarioActivoConfirmation : IUserConfirmation<Usuario>
{
    public Task<bool> IsConfirmedAsync(UserManager<Usuario> manager, Usuario user)
        => Task.FromResult(user.Activo);
}
