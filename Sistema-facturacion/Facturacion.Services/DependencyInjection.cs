using Microsoft.Extensions.DependencyInjection;
using Facturacion.Services.Clientes;
using Facturacion.Services.Productos;
using Facturacion.Services.Usuarios;

namespace Facturacion.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        return services;
    }
}
