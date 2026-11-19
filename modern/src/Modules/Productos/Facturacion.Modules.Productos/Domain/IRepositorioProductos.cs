namespace Facturacion.Modules.Productos.Domain;

public interface IRepositorioProductos
{
    Task<IReadOnlyList<Producto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Producto>> ListarActivosAsync(CancellationToken cancellationToken = default);

    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Producto?> ObtenerPorCodigoAsync(string codigoNormalizado, CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoAsync(string codigoNormalizado, int exceptoId = 0, CancellationToken cancellationToken = default);

    Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

