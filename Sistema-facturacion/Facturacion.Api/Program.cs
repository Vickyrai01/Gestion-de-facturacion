using Facturacion.Api;
using Facturacion.Api.Grpc;
using Facturacion.Api.Seguridad;
using Facturacion.Infrastructure;
using Facturacion.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Facturación API",
        Version = "v1"
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
// Clase 6: Identity sobre USUARIOS, cookie/bearer y políticas por operación.
builder.Services.AddSeguridad(builder.Configuration);
builder.Services.AddHostedService<FacturacionWorker>();

var app = builder.Build();

// Deja constancia inequívoca del binario y la configuración efectiva. Esto
// permite detectar inmediatamente si se está ejecutando una salida antigua.
app.Logger.LogInformation(
    "Facturacion.Api runtime marker {Marker}; Assembly={Assembly}; Environment={Environment}; Urls={Urls}",
    "openapi-swagger-http-v2",
    typeof(Program).Assembly.Location,
    app.Environment.EnvironmentName,
    string.Join(",", app.Urls));

// Los documentos se publican en todos los entornos para que los perfiles
// http/https de launchSettings.json y los despliegues no dependan del entorno.
app.MapOpenApi().AllowAnonymous();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Facturación API v1");
    options.RoutePrefix = "swagger";
});

// Orden importante: primero se identifica al usuario (autenticación) y después
// se decide si puede ejecutar el endpoint (autorización).
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// No forzar una redirección aquí: el perfil http expone deliberadamente el
// puerto 5246 y debe permitir probar /openapi/v1.json y /swagger en HTTP.
app.MapControllers();
app.MapGrpcService<ClientesGrpcService>()
    .RequireAuthorization(Politicas.Consultar);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
