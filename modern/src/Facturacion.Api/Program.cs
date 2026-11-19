using Facturacion.BuildingBlocks.Aplicacion;
using Facturacion.BuildingBlocks.Infrastructure;
using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Clientes;
using Facturacion.Modules.Facturacion;
using Facturacion.Modules.Productos;
using Facturacion.Modules.Usuarios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1. Servicios transversales del kernel compartido
builder.Services.AddSingleton<IReloj, RelojSistema>();
builder.Services.AddProblemDetails();

// 2. Descubrimiento de controllers ubicados en la capa Presentation de cada módulo
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(ClientesModule).Assembly)
    .AddApplicationPart(typeof(ProductosModule).Assembly)
    .AddApplicationPart(typeof(FacturacionModule).Assembly)
    .AddApplicationPart(typeof(UsuariosModule).Assembly);

builder.Services.AddOpenApi();

// 3. Registro autónomo de cada módulo del Monolito Modular
builder.Services.AddModuloClientes(builder.Configuration);
builder.Services.AddModuloProductos(builder.Configuration);
builder.Services.AddModuloFacturacion(builder.Configuration);
builder.Services.AddModuloUsuarios(builder.Configuration);

var app = builder.Build();

// 4. Traducción transversal de conflictos de concurrencia / índices únicos a HTTP 409
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is ConflictoDePersistenciaException conflicto)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = conflicto.Message,
                Detail = conflicto.Message
            });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error interno al procesar la solicitud."
        });
    });
});

// 5. Inicialización de datos demo cuando se ejecuta con base en memoria para el laboratorio
var proveedorPersistencia = app.Configuration["Persistencia:Proveedor"] ?? PersistenciaModulosExtensions.ProveedorInMemory;
if (string.Equals(proveedorPersistencia, PersistenciaModulosExtensions.ProveedorInMemory, StringComparison.OrdinalIgnoreCase))
{
    await ClientesModule.SeedDatosDemoAsync(app.Services);
    await ProductosModule.SeedDatosDemoAsync(app.Services);
    await UsuariosModule.SeedDatosDemoAsync(app.Services);
    await FacturacionModule.SeedDatosDemoAsync(app.Services);
}

app.MapOpenApi().AllowAnonymous();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Sistema de Facturación — Monolito Modular (.NET 10)");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    sistema = "Sistema de Gestión de Facturación",
    clase = "Clase 7 — Refactor de Arquitectura .NET",
    arquitectura = "Monolito Modular + Clean Architecture + DDD",
    modulos = new[] { "Clientes", "Productos", "Facturacion", "Usuarios" },
    persistencia = proveedorPersistencia,
    documentacion = "/swagger"
})).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program;

