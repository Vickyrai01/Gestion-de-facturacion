using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Productos.Domain;

/// <summary>
/// Errores del módulo Productos con los mismos mensajes del legacy.
/// </summary>
public static class ErroresProducto
{
    public static readonly Error CodigoObligatorio =
        Error.Validacion("Productos.CodigoObligatorio", "El código es obligatorio.");

    public static readonly Error CodigoMuyLargo =
        Error.Validacion("Productos.CodigoMuyLargo", "El código no puede superar los 30 caracteres.");

    public static readonly Error NombreObligatorio =
        Error.Validacion("Productos.NombreObligatorio", "El nombre es obligatorio.");

    public static readonly Error NombreMuyLargo =
        Error.Validacion("Productos.NombreMuyLargo", "El nombre no puede superar los 100 caracteres.");

    public static readonly Error DescripcionMuyLarga =
        Error.Validacion("Productos.DescripcionMuyLarga", "La descripción no puede superar los 500 caracteres.");

    public static readonly Error PrecioFueraDeRango =
        Error.Validacion("Productos.PrecioFueraDeRango", "El precio debe ser un importe no negativo, hasta 9999999999.");

    public static readonly Error StockNegativo =
        Error.Validacion("Productos.StockNegativo", "El stock no puede ser negativo.");

    public static readonly Error CodigoDuplicado =
        Error.Conflicto("Productos.CodigoDuplicado", "Ya existe un producto con ese código.");

    public static Error NoEncontrado(int id) =>
        Error.NoEncontrado("Productos.NoEncontrado", $"No se encontró el producto con identificador {id}.");
}

