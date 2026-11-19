using Facturacion.BuildingBlocks.Aplicacion;
using Facturacion.BuildingBlocks.Resultados;
using Facturacion.Modules.Clientes.Contracts;
using Facturacion.Modules.Facturacion.Domain;
using Facturacion.Modules.Productos.Contracts;
using Microsoft.Extensions.Options;

namespace Facturacion.Modules.Facturacion.Application;

/// <summary>
/// Configuración tipada del módulo Facturación.
/// Reemplaza a `ConfigurationManager.AppSettings["TasaImpuestos"]` que en el legacy
/// se leía estáticamente dentro de `FacturasController.ObtenerTasa()`, y a
/// `ConfigurationManager.AppSettings["IntervaloSegundos"]` de `ResumenPendientes.cs`.
/// </summary>
public sealed class OpcionesFacturacion
{
    public const string Seccion = "Facturacion";

    public decimal TasaImpuestos { get; set; } = 0.21m;

    public int IntervaloResumenSegundos { get; set; } = 60;
}

public sealed record CrearFacturaComando(
    int ClienteId,
    IReadOnlyList<LineaFacturaSolicitud>? Lineas,
    decimal? TasaImpuestos = null);

public sealed record FacturaResumenDto(
    int Id,
    string Numero,
    int ClienteId,
    string ClienteNombre,
    DateTime FechaEmision,
    string Estado,
    decimal Subtotal,
    decimal Impuestos,
    decimal Total);

public sealed record DetalleFacturaDto(
    int ProductoId,
    string CodigoProducto,
    string NombreProducto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

public sealed record FacturaDetalleDto(
    int Id,
    string Numero,
    int ClienteId,
    string ClienteNombre,
    DateTime FechaEmision,
    string Estado,
    decimal Subtotal,
    decimal Impuestos,
    decimal Total,
    IReadOnlyList<DetalleFacturaDto> Detalles);

public sealed record CatalogoFacturacionDto(
    decimal TasaImpuestos,
    IReadOnlyList<ClienteCatalogoDto> ClientesActivos,
    IReadOnlyList<ProductoCatalogoDto> ProductosActivos,
    IReadOnlyList<string> EstadosDisponibles);

public interface IServicioFacturacion
{
    Task<Result<IReadOnlyList<FacturaResumenDto>>> ConsultarAsync(
        int? clienteId = null,
        string? estado = null,
        CancellationToken cancellationToken = default);

    Task<Result<FacturaDetalleDto>> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<FacturaDetalleDto>> CrearAsync(
        CrearFacturaComando comando,
        CancellationToken cancellationToken = default);

    Task<Result<FacturaDetalleDto>> EmitirAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<FacturaDetalleDto>> AnularAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResumenEstadoFacturas>> ReportePorEstadoAsync(
        CancellationToken cancellationToken = default);

    Task<int> ContarPendientesAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogoFacturacionDto> ObtenerCatalogoFormularioAsync(
        CancellationToken cancellationToken = default);
}

internal sealed class ServicioFacturacion(
    IRepositorioFacturas repositorioFacturas,
    IConsultaClientes consultaClientes,
    IConsultaProductos consultaProductos,
    IReloj reloj,
    IOptions<OpcionesFacturacion> opciones) : IServicioFacturacion
{
    public async Task<Result<IReadOnlyList<FacturaResumenDto>>> ConsultarAsync(
        int? clienteId = null,
        string? estado = null,
        CancellationToken cancellationToken = default)
    {
        string? estadoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            estadoNormalizado = EstadosFactura.Normalizar(estado);
            if (estadoNormalizado is null)
                return ErroresFactura.EstadoFiltroInvalido;
        }

        var facturas = await repositorioFacturas.ConsultarAsync(clienteId, estadoNormalizado, cancellationToken);
        if (facturas.Count == 0)
            return Array.Empty<FacturaResumenDto>();

        var resultado = new List<FacturaResumenDto>(facturas.Count);
        var cacheClientes = new Dictionary<int, string>();

        foreach (var factura in facturas)
        {
            if (!cacheClientes.TryGetValue(factura.ClienteId, out var nombreCliente))
            {
                var cliente = await consultaClientes.ObtenerPorIdAsync(factura.ClienteId, cancellationToken);
                nombreCliente = cliente?.NombreCompleto ?? $"Cliente #{factura.ClienteId}";
                cacheClientes[factura.ClienteId] = nombreCliente;
            }

            resultado.Add(new FacturaResumenDto(
                factura.Id,
                factura.Numero,
                factura.ClienteId,
                nombreCliente,
                factura.FechaEmision,
                factura.Estado,
                factura.Subtotal,
                factura.Impuestos,
                factura.Total));
        }

        return resultado;
    }

    public async Task<Result<FacturaDetalleDto>> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return ErroresFactura.NoEncontrada(id);

        var factura = await repositorioFacturas.ObtenerPorIdAsync(id, cancellationToken);
        if (factura is null)
            return ErroresFactura.NoEncontrada(id);

        return await ArmarDetalleDtoAsync(factura, cancellationToken);
    }

    public async Task<Result<FacturaDetalleDto>> CrearAsync(
        CrearFacturaComando comando,
        CancellationToken cancellationToken = default)
    {
        var tasa = comando.TasaImpuestos ?? opciones.Value.TasaImpuestos;
        var validacionPreliminar = Factura.ValidarSolicitud(comando.ClienteId, comando.Lineas, tasa);
        if (validacionPreliminar.EsFallo)
            return validacionPreliminar.Error;

        if (!await consultaClientes.EstaActivoAsync(comando.ClienteId, cancellationToken))
            return ErroresFactura.ClienteInvalido;

        var idsProductos = comando.Lineas!.Select(l => l.ProductoId).Distinct().ToArray();
        var productos = await consultaProductos.ObtenerPorIdsAsync(idsProductos, cancellationToken);

        if (productos.Count != idsProductos.Length || productos.Values.Any(p => !p.Activo))
            return ErroresFactura.ProductoInvalido;

        var preciosVigentes = productos.Values.ToDictionary(
            p => p.Id,
            p => new PrecioProductoVigente(p.Id, p.PrecioUnitario));

        // Pre-validamos el cálculo del dominio antes de consumir un número correlativo.
        var precalculo = Factura.Crear(
            "FAC-0000000000",
            comando.ClienteId,
            reloj.AhoraUtc,
            comando.Lineas,
            preciosVigentes,
            tasa);

        if (precalculo.EsFallo)
            return precalculo.Error;

        var facturaCreada = await repositorioFacturas.CrearConNumeroCorrelativoAsync(
            (numero, _) => Task.FromResult(
                Factura.Crear(
                    numero,
                    comando.ClienteId,
                    reloj.AhoraUtc,
                    comando.Lineas,
                    preciosVigentes,
                    tasa).Valor),
            cancellationToken);

        return await ArmarDetalleDtoAsync(facturaCreada, cancellationToken, productos);
    }

    public async Task<Result<FacturaDetalleDto>> EmitirAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var factura = await repositorioFacturas.ObtenerPorIdAsync(id, cancellationToken);
        if (factura is null)
            return ErroresFactura.NoEncontrada(id);

        var transicion = factura.Emitir();
        if (transicion.EsFallo)
            return transicion.Error;

        await repositorioFacturas.GuardarCambiosAsync(cancellationToken);
        return await ArmarDetalleDtoAsync(factura, cancellationToken);
    }

    public async Task<Result<FacturaDetalleDto>> AnularAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var factura = await repositorioFacturas.ObtenerPorIdAsync(id, cancellationToken);
        if (factura is null)
            return ErroresFactura.NoEncontrada(id);

        var transicion = factura.Anular();
        if (transicion.EsFallo)
            return transicion.Error;

        await repositorioFacturas.GuardarCambiosAsync(cancellationToken);
        return await ArmarDetalleDtoAsync(factura, cancellationToken);
    }

    public Task<IReadOnlyList<ResumenEstadoFacturas>> ReportePorEstadoAsync(
        CancellationToken cancellationToken = default) =>
        repositorioFacturas.ObtenerResumenPorEstadoAsync(cancellationToken);

    public Task<int> ContarPendientesAsync(
        CancellationToken cancellationToken = default) =>
        repositorioFacturas.ContarPendientesAsync(cancellationToken);

    public async Task<CatalogoFacturacionDto> ObtenerCatalogoFormularioAsync(
        CancellationToken cancellationToken = default)
    {
        var clientes = await consultaClientes.ListarActivosAsync(cancellationToken);
        var productos = await consultaProductos.ListarActivosAsync(cancellationToken);
        return new CatalogoFacturacionDto(
            opciones.Value.TasaImpuestos,
            clientes,
            productos,
            EstadosFactura.Todos);
    }

    private async Task<FacturaDetalleDto> ArmarDetalleDtoAsync(
        Factura factura,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<int, ProductoCatalogoDto>? productosPreCargados = null)
    {
        var cliente = await consultaClientes.ObtenerPorIdAsync(factura.ClienteId, cancellationToken);
        var productos = productosPreCargados
            ?? await consultaProductos.ObtenerPorIdsAsync(
                factura.Detalles.Select(d => d.ProductoId).Distinct().ToArray(),
                cancellationToken);

        var detallesDto = factura.Detalles
            .Select(d =>
            {
                productos.TryGetValue(d.ProductoId, out var prod);
                return new DetalleFacturaDto(
                    d.ProductoId,
                    prod?.Codigo ?? $"ID-{d.ProductoId}",
                    prod?.Nombre ?? $"Producto #{d.ProductoId}",
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal);
            })
            .ToList();

        return new FacturaDetalleDto(
            factura.Id,
            factura.Numero,
            factura.ClienteId,
            cliente?.NombreCompleto ?? $"Cliente #{factura.ClienteId}",
            factura.FechaEmision,
            factura.Estado,
            factura.Subtotal,
            factura.Impuestos,
            factura.Total,
            detallesDto);
    }
}

