namespace Facturacion.Modules.Productos.Contracts;

/// <summary>
/// Instantánea de un producto del catálogo que otros módulos (Facturación)
/// usan para tomar el precio vigente, el código y verificar si está activo.
/// </summary>
public sealed record ProductoCatalogoDto(
    int Id,
    string Codigo,
    string Nombre,
    decimal PrecioUnitario,
    int Stock,
    bool Activo);

/// <summary>
/// Puerto de comunicación entre módulos para consultar el catálogo de productos.
/// </summary>
public interface IConsultaProductos
{
    Task<IReadOnlyDictionary<int, ProductoCatalogoDto>> ObtenerPorIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductoCatalogoDto>> ListarActivosAsync(
        CancellationToken cancellationToken = default);
}

