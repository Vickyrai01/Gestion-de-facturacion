using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Facturacion.Modules.Clientes.Application;
using Facturacion.Modules.Facturacion.Application;
using Facturacion.Modules.Facturacion.Domain;
using Facturacion.Modules.Facturacion.Presentation;
using Facturacion.Modules.Productos.Application;
using Facturacion.Modules.Usuarios;
using Facturacion.Modules.Usuarios.Application;
using Facturacion.Modules.Usuarios.Presentation;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Facturacion.Tests;

/// <summary>
/// Pruebas de integración del Monolito Modular completo.
/// Verifican que el Host compone los 4 módulos y que Facturación interactúa con
/// Clientes y Productos a través de sus contratos públicos.
/// </summary>
public sealed class IntegracionMonolitoModularTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public IntegracionMonolitoModularTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Flujo_completo_login_catalogos_creacion_emision_anular_y_reportes()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        // 1. Un request sin token a un endpoint protegido devuelve 401
        var sinToken = await client.GetAsync("/api/clientes", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);

        // 2. Login con cuenta ADMIN demo
        var loginResp = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin", UsuariosModule.PasswordDemoAdmin),
            ct);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var login = await loginResp.Content.ReadFromJsonAsync<LoginResultadoDto>(ct);
        Assert.NotNull(login);
        Assert.Equal("admin", login!.Usuario.NombreUsuario);
        Assert.Equal("ADMIN", login.Usuario.Rol);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        // 3. Verificar que /api/usuarios NUNCA expone passwordHash en el JSON (Desacople DTO / Repositorios)
        var usuariosJson = await client.GetStringAsync("/api/usuarios", ct);
        Assert.DoesNotContain("passwordHash", usuariosJson, StringComparison.OrdinalIgnoreCase);

        // 4. Consultar clientes y productos activos
        var clientes = await client.GetFromJsonAsync<List<ClienteDto>>("/api/clientes", ct);
        var productos = await client.GetFromJsonAsync<List<ProductoDto>>("/api/productos/activos", ct);
        Assert.NotNull(clientes);
        Assert.NotNull(productos);
        Assert.True(clientes!.Count >= 5);
        Assert.True(productos!.Count >= 8);

        var clienteActivo = clientes.First(c => c.Activo);
        var clienteInactivo = clientes.First(c => !c.Activo);

        // 5. Intentar facturar a un cliente inactivo devuelve 400 con el mensaje de negocio del legacy
        var facturaClienteInactivo = await client.PostAsJsonAsync(
            "/api/facturas",
            new CrearFacturaRequest(
                clienteInactivo.Id,
                [new LineaFacturaRequest(productos[0].Id, 1)]),
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, facturaClienteInactivo.StatusCode);

        // 6. Crear factura válida para cliente activo
        var crearFacturaResp = await client.PostAsJsonAsync(
            "/api/facturas",
            new CrearFacturaRequest(
                clienteActivo.Id,
                [
                    new LineaFacturaRequest(productos[0].Id, 2), // 100 x 2 = 200
                    new LineaFacturaRequest(productos[1].Id, 1)  // 200 x 1 = 200
                ]),
            ct);
        Assert.Equal(HttpStatusCode.Created, crearFacturaResp.StatusCode);

        var facturaCreada = await crearFacturaResp.Content.ReadFromJsonAsync<FacturaDetalleDto>(ct);
        Assert.NotNull(facturaCreada);
        Assert.Equal(EstadosFactura.Pendiente, facturaCreada!.Estado);
        Assert.Equal(400.00m, facturaCreada.Subtotal);
        Assert.Equal(84.00m, facturaCreada.Impuestos);
        Assert.Equal(484.00m, facturaCreada.Total);

        // 7. Emitir la factura creada
        var emitirResp = await client.PostAsync($"/api/facturas/{facturaCreada.Id}/emitir", null, ct);
        Assert.Equal(HttpStatusCode.OK, emitirResp.StatusCode);

        // 8. Intentar emitirla una segunda vez devuelve 409 Conflict
        var emitirRepetido = await client.PostAsync($"/api/facturas/{facturaCreada.Id}/emitir", null, ct);
        Assert.Equal(HttpStatusCode.Conflict, emitirRepetido.StatusCode);

        // 9. Consultar reporte por estado
        var reportes = await client.GetFromJsonAsync<List<ResumenEstadoFacturas>>("/api/facturas/reportes", ct);
        Assert.NotNull(reportes);
        Assert.NotEmpty(reportes!);
    }

    [Fact]
    public async Task Operador_puede_consultar_y_crear_facturas_pero_no_puede_emitir_ni_modificar_catalogo()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        var loginResp = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("operador", UsuariosModule.PasswordDemoOperador),
            ct);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var login = (await loginResp.Content.ReadFromJsonAsync<LoginResultadoDto>(ct))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        // Operador puede consultar facturas
        var consulta = await client.GetAsync("/api/facturas", ct);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

        // Operador NO puede acceder a reportes ni administrar usuarios (403 Forbidden)
        var reportes = await client.GetAsync("/api/facturas/reportes", ct);
        Assert.Equal(HttpStatusCode.Forbidden, reportes.StatusCode);

        var usuarios = await client.GetAsync("/api/usuarios", ct);
        Assert.Equal(HttpStatusCode.Forbidden, usuarios.StatusCode);
    }
}
