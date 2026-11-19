using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Facturacion.Domain;

/// <summary>
/// Estados válidos de una factura (idénticos a EstadosFactura del legacy en la base SQL).
/// </summary>
public static class EstadosFactura
{
    public const string Pendiente = "PENDIENTE";
    public const string Emitida = "EMITIDA";
    public const string Anulada = "ANULADA";

    public static readonly IReadOnlyList<string> Todos = [Pendiente, Emitida, Anulada];

    public static string? Normalizar(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return null;

        var valor = estado.Trim().ToUpperInvariant();
        return Todos.Contains(valor) ? valor : null;
    }
}

/// <summary>
/// Catálogo de errores de negocio de Facturación (mismos textos del legacy).
/// </summary>
public static class ErroresFactura
{
    public static readonly Error CantidadDetallesInvalida =
        Error.Validacion("Facturas.CantidadDetallesInvalida", "La factura debe contener entre 1 y 100 detalles.");

    public static readonly Error LineaInvalida =
        Error.Validacion("Facturas.LineaInvalida", "Seleccioná productos y cantidades positivas, hasta 1000000 por detalle.");

    public static readonly Error TasaInvalida =
        Error.Validacion("Facturas.TasaInvalida", "La tasa didáctica debe estar entre 0 y 1.");

    public static readonly Error ClienteInvalido =
        Error.Validacion("Facturas.ClienteInvalido", "El cliente no existe o está inactivo.");

    public static readonly Error ProductoInvalido =
        Error.Validacion("Facturas.ProductoInvalido", "Uno de los productos no existe o está inactivo.");

    public static readonly Error ImporteExcedeCapacidad =
        Error.Validacion("Facturas.ImporteExcedeCapacidad", "El importe excede la capacidad de la base de datos.");

    public static readonly Error EstadoFiltroInvalido =
        Error.Validacion("Facturas.EstadoFiltroInvalido", "Estado inválido.");

    public static readonly Error NumeroObligatorio =
        Error.Validacion("Facturas.NumeroObligatorio", "El número de factura es obligatorio.");

    public static readonly Error SoloPendienteSePuedeEmitir =
        Error.Conflicto("Facturas.SoloPendienteSePuedeEmitir", "Solo se puede emitir una factura pendiente.");

    public static readonly Error YaEstaAnulada =
        Error.Conflicto("Facturas.YaEstaAnulada", "La factura ya está anulada.");

    public static Error NoEncontrada(int id) =>
        Error.NoEncontrado("Facturas.NoEncontrada", $"Factura inexistente (id {id}).");
}

