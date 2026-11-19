using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Productos.Contracts;
using Facturacion.Modules.Productos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Modules.Productos.Infrastructure;

/// <summary>
/// DbContext exclusivo del módulo Productos.
/// Sólo mapea la tabla `Productos` y su índice único `UX_Producto_Codigo`.
/// </summary>
internal sealed class ProductosDbContext(DbContextOptions<ProductosDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Codigo, "UX_Producto_Codigo").IsUnique();
            entity.Property(p => p.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Descripcion).HasMaxLength(500);
            entity.Property(p => p.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(p => p.Stock).IsRequired();
            entity.Property(p => p.Activo).IsRequired();
        });
    }
}

internal sealed class RepositorioProductosEf(ProductosDbContext db) : IRepositorioProductos
{
    public async Task<IReadOnlyList<Producto>> ListarAsync(CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking().OrderBy(p => p.Codigo).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Producto>> ListarActivosAsync(CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Codigo).ToListAsync(cancellationToken);

    public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Productos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Producto?> ObtenerPorCodigoAsync(string codigoNormalizado, CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.Codigo == codigoNormalizado, cancellationToken);

    public async Task<bool> ExisteCodigoAsync(string codigoNormalizado, int exceptoId = 0, CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking().AnyAsync(p => p.Codigo == codigoNormalizado && p.Id != exceptoId, cancellationToken);

    public async Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default) =>
        await db.Productos.AddAsync(producto, cancellationToken);

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        PersistenciaModulosExtensions.GuardarManejandoConflictosAsync(
            db,
            ErroresProducto.CodigoDuplicado.Mensaje,
            cancellationToken);
}

internal sealed class ConsultaProductosModulo(ProductosDbContext db) : IConsultaProductos
{
    public async Task<IReadOnlyDictionary<int, ProductoCatalogoDto>> ObtenerPorIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return new Dictionary<int, ProductoCatalogoDto>();

        var distintos = ids.Distinct().ToArray();
        return await db.Productos
            .AsNoTracking()
            .Where(p => distintos.Contains(p.Id))
            .Select(p => new ProductoCatalogoDto(p.Id, p.Codigo, p.Nombre, p.PrecioUnitario, p.Stock, p.Activo))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductoCatalogoDto>> ListarActivosAsync(CancellationToken cancellationToken = default) =>
        await db.Productos
            .AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Codigo)
            .Select(p => new ProductoCatalogoDto(p.Id, p.Codigo, p.Nombre, p.PrecioUnitario, p.Stock, p.Activo))
            .ToListAsync(cancellationToken);
}

