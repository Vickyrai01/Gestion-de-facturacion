using Facturacion.BuildingBlocks.Dominio;
using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Facturacion.Domain;

/// <summary>
/// Línea solicitada al crear una factura (producto y cantidad).
/// </summary>
public sealed record LineaFacturaSolicitud(int ProductoId, int Cantidad);

/// <summary>
/// Precio vigente de un producto activo verificado para facturar.
/// </summary>
public sealed record PrecioProductoVigente(int ProductoId, decimal PrecioUnitario);

/// <summary>
/// Entidad hija dentro del agregado <see cref="Factura"/>.
/// No existe ni se modifica fuera de su Factura.
/// </summary>
public sealed class DetalleFactura : Entidad
{
    private DetalleFactura() { }

    internal DetalleFactura(int productoId, int cantidad, decimal precioUnitario, decimal subtotal)
    {
        ProductoId = productoId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        Subtotal = subtotal;
    }

    public int FacturaId { get; private set; }
    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Subtotal { get; private set; }
}

/// <summary>
/// Aggregate Root de Facturación (DDD).
///
/// En el legacy:
/// - `Factura` era una bolsa de getters/setters públicos (`legacy/FacturacionLegacy.Datos/Entidades/Factura.cs`).
/// - Los cálculos y validaciones estaban en un método estático procedural (`FacturacionOperaciones.Crear`)
///   mezclado con `BeginTransaction(IsolationLevel.Serializable)` y `SELECT NEXT VALUE FOR`.
/// - Las transiciones `Emitir` y `Anular` eran `UPDATE dbo.Facturas SET Estado = ...` directos en SQL.
///
/// En Clean Architecture + DDD:
/// - `Factura` es la raíz del agregado: protege sus invariantes, calcula subtotales/impuestos/total con
///   `MidpointRounding.AwayFromZero` y gobierna las transiciones `Emitir()` y `Anular()`.
/// - Se puede instanciar y probar en microsegundos sin base de datos.
/// </summary>
public sealed class Factura : Entidad
{
    public const int MaximoDetalles = 100;
    public const int MaximoCantidadPorDetalle = 1_000_000;
    public const decimal ImporteMaximo = 9_999_999_999_999_999.99m;

    private readonly List<DetalleFactura> detalles = [];

    private Factura() { }

    private Factura(
        string numero,
        int clienteId,
        DateTime fechaEmision,
        string estado,
        decimal subtotal,
        decimal impuestos,
        decimal total,
        List<DetalleFactura> detallesCalculados)
    {
        Numero = numero;
        ClienteId = clienteId;
        FechaEmision = fechaEmision;
        Estado = estado;
        Subtotal = subtotal;
        Impuestos = impuestos;
        Total = total;
        detalles = detallesCalculados;
    }

    public string Numero { get; private set; } = string.Empty;
    public int ClienteId { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public string Estado { get; private set; } = EstadosFactura.Pendiente;
    public decimal Subtotal { get; private set; }
    public decimal Impuestos { get; private set; }
    public decimal Total { get; private set; }

    public IReadOnlyCollection<DetalleFactura> Detalles => detalles.AsReadOnly();

    /// <summary>
    /// Valida los datos de entrada antes de abrir transacciones o consultar catálogos.
    /// </summary>
    public static Result ValidarSolicitud(
        int clienteId,
        IReadOnlyList<LineaFacturaSolicitud>? lineas,
        decimal tasaImpuestos)
    {
        if (lineas is null || lineas.Count == 0 || lineas.Count > MaximoDetalles)
            return ErroresFactura.CantidadDetallesInvalida;

        if (lineas.Any(l => l is null || l.ProductoId <= 0 || l.Cantidad <= 0 || l.Cantidad > MaximoCantidadPorDetalle))
            return ErroresFactura.LineaInvalida;

        if (tasaImpuestos < 0m || tasaImpuestos > 1m)
            return ErroresFactura.TasaInvalida;

        if (clienteId <= 0)
            return ErroresFactura.ClienteInvalido;

        return Result.Exito();
    }

    /// <summary>
    /// Fábrica del agregado: construye la factura en estado PENDIENTE con sus detalles y totales calculados.
    /// </summary>
    public static Result<Factura> Crear(
        string numero,
        int clienteId,
        DateTime fechaEmisionUtc,
        IReadOnlyList<LineaFacturaSolicitud>? lineas,
        IReadOnlyDictionary<int, PrecioProductoVigente> preciosVigentes,
        decimal tasaImpuestos)
    {
        var validacionNumero = Validar.TextoRequerido(
            numero, 30, ErroresFactura.NumeroObligatorio, ErroresFactura.NumeroObligatorio);
        if (validacionNumero.EsFallo)
            return validacionNumero.Error;

        var validacionSolicitud = ValidarSolicitud(clienteId, lineas, tasaImpuestos);
        if (validacionSolicitud.EsFallo)
            return validacionSolicitud.Error;

        var detallesCalculados = new List<DetalleFactura>(lineas!.Count);
        foreach (var linea in lineas)
        {
            if (!preciosVigentes.TryGetValue(linea.ProductoId, out var producto))
                return ErroresFactura.ProductoInvalido;

            var precio = Dinero.Redondear(producto.PrecioUnitario);
            var subtotalLinea = Dinero.Redondear(precio * linea.Cantidad);
            detallesCalculados.Add(new DetalleFactura(linea.ProductoId, linea.Cantidad, precio, subtotalLinea));
        }

        var subtotal = detallesCalculados.Sum(d => d.Subtotal);
        var impuestos = Dinero.Redondear(subtotal * tasaImpuestos);
        var total = subtotal + impuestos;

        if (total > ImporteMaximo)
            return ErroresFactura.ImporteExcedeCapacidad;

        return new Factura(
            validacionNumero.Valor,
            clienteId,
            DateTime.SpecifyKind(fechaEmisionUtc, DateTimeKind.Utc),
            EstadosFactura.Pendiente,
            subtotal,
            impuestos,
            total,
            detallesCalculados);
    }

    public static string FormatearNumero(long correlativo) => $"FAC-{correlativo:D10}";

    /// <summary>
    /// Regla de negocio: sólo una factura PENDIENTE puede pasar a EMITIDA.
    /// </summary>
    public Result Emitir()
    {
        if (Estado != EstadosFactura.Pendiente)
            return ErroresFactura.SoloPendienteSePuedeEmitir;

        Estado = EstadosFactura.Emitida;
        return Result.Exito();
    }

    /// <summary>
    /// Regla de negocio: una factura PENDIENTE o EMITIDA puede anularse; si ya está ANULADA devuelve conflicto.
    /// </summary>
    public Result Anular()
    {
        if (Estado == EstadosFactura.Anulada)
            return ErroresFactura.YaEstaAnulada;

        Estado = EstadosFactura.Anulada;
        return Result.Exito();
    }
}

