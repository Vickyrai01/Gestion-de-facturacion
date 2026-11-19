using Facturacion.BuildingBlocks.Resultados;
using Facturacion.Modules.Productos.Domain;

namespace Facturacion.Modules.Productos.Application;

public sealed record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    decimal PrecioUnitario,
    int Stock,
    bool Activo);

public sealed record GuardarProductoComando(
    string? Codigo,
    string? Nombre,
    string? Descripcion,
    decimal PrecioUnitario,
    int Stock);

public interface IServicioProductos
{
    Task<IReadOnlyList<ProductoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductoDto>> ListarActivosAsync(CancellationToken cancellationToken = default);

    Task<Result<ProductoDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ProductoDto>> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task<Result<ProductoDto>> CrearAsync(GuardarProductoComando comando, CancellationToken cancellationToken = default);

    Task<Result<ProductoDto>> ActualizarAsync(int id, GuardarProductoComando comando, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(int id, CancellationToken cancellationToken = default);
}

internal sealed class ServicioProductos(IRepositorioProductos repositorio) : IServicioProductos
{
    public async Task<IReadOnlyList<ProductoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var productos = await repositorio.ListarAsync(cancellationToken);
        return productos.Select(ADto).ToList();
    }

    public async Task<IReadOnlyList<ProductoDto>> ListarActivosAsync(CancellationToken cancellationToken = default)
    {
        var productos = await repositorio.ListarActivosAsync(cancellationToken);
        return productos.Select(ADto).ToList();
    }

    public async Task<Result<ProductoDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        return producto is null ? ErroresProducto.NoEncontrado(id) : ADto(producto);
    }

    public async Task<Result<ProductoDto>> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var codigoValido = CodigoProducto.Crear(codigo);
        if (codigoValido.EsFallo)
            return codigoValido.Error;

        var producto = await repositorio.ObtenerPorCodigoAsync(codigoValido.Valor.Valor, cancellationToken);
        return producto is null
            ? Error.NoEncontrado("Productos.CodigoNoEncontrado", $"No se encontró un producto con código '{codigoValido.Valor.Valor}'.")
            : ADto(producto);
    }

    public async Task<Result<ProductoDto>> CrearAsync(GuardarProductoComando comando, CancellationToken cancellationToken = default)
    {
        var creacion = Producto.Crear(
            comando.Codigo,
            comando.Nombre,
            comando.Descripcion,
            comando.PrecioUnitario,
            comando.Stock);

        if (creacion.EsFallo)
            return creacion.Error;

        var producto = creacion.Valor;
        if (await repositorio.ExisteCodigoAsync(producto.Codigo, exceptoId: 0, cancellationToken))
            return ErroresProducto.CodigoDuplicado;

        await repositorio.AgregarAsync(producto, cancellationToken);
        await repositorio.GuardarCambiosAsync(cancellationToken);
        return ADto(producto);
    }

    public async Task<Result<ProductoDto>> ActualizarAsync(int id, GuardarProductoComando comando, CancellationToken cancellationToken = default)
    {
        var actual = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (actual is null)
            return ErroresProducto.NoEncontrado(id);

        var actualizacion = actual.Actualizar(
            comando.Codigo,
            comando.Nombre,
            comando.Descripcion,
            comando.PrecioUnitario,
            comando.Stock);

        if (actualizacion.EsFallo)
            return actualizacion.Error;

        if (await repositorio.ExisteCodigoAsync(actual.Codigo, exceptoId: actual.Id, cancellationToken))
            return ErroresProducto.CodigoDuplicado;

        await repositorio.GuardarCambiosAsync(cancellationToken);
        return ADto(actual);
    }

    public async Task<Result> DesactivarAsync(int id, CancellationToken cancellationToken = default)
    {
        var actual = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (actual is null)
            return ErroresProducto.NoEncontrado(id);

        actual.Desactivar();
        await repositorio.GuardarCambiosAsync(cancellationToken);
        return Result.Exito();
    }

    internal static ProductoDto ADto(Producto producto) =>
        new(
            producto.Id,
            producto.Codigo,
            producto.Nombre,
            producto.Descripcion,
            producto.PrecioUnitario,
            producto.Stock,
            producto.Activo);
}

