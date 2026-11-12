using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Infrastructure.Persistence.Repository;

namespace Facturacion.Services.Productos;

public sealed class ProductoService : IProductoService
{
    private readonly IUnitOfWork unitOfWork;

    public ProductoService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<IReadOnlyList<Producto>> GetAllAsync(CancellationToken cancellationToken = default)
        => unitOfWork.Productos.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Producto>> GetActivosAsync(CancellationToken cancellationToken = default)
        => unitOfWork.Productos.GetActivosAsync(cancellationToken);

    public async Task<Producto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await unitOfWork.Productos.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el producto con id {id}.");

    public Task<Producto?> GetByCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        return unitOfWork.Productos.GetByCodigoAsync(codigo.Trim(), cancellationToken);
    }

    public async Task<Producto> CreateAsync(
        Producto producto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(producto);
        Validate(producto);

        if (await GetByCodigoAsync(producto.Codigo, cancellationToken) is not null)
            throw new InvalidOperationException(
                $"Ya existe un producto con el código '{producto.Codigo}'.");

        await unitOfWork.Productos.AddAsync(producto, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return producto;
    }

    public async Task<Producto> UpdateAsync(
        Producto producto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(producto);
        Validate(producto);
        _ = await GetByIdAsync(producto.Id, cancellationToken);

        var productoConMismoCodigo = await GetByCodigoAsync(producto.Codigo, cancellationToken);
        if (productoConMismoCodigo is not null && productoConMismoCodigo.Id != producto.Id)
            throw new InvalidOperationException(
                $"Ya existe un producto con el código '{producto.Codigo}'.");

        unitOfWork.Productos.Update(producto);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return producto;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await GetByIdAsync(id, cancellationToken);
        unitOfWork.Productos.Delete(producto);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(Producto producto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(producto.Codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(producto.Nombre);
        ArgumentOutOfRangeException.ThrowIfNegative(producto.Preciounitario);
        ArgumentOutOfRangeException.ThrowIfNegative(producto.Stock);
    }
}
