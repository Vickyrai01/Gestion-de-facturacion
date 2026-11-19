using System.Data;
using Facturacion.Modules.Facturacion.Domain;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Modules.Facturacion.Infrastructure;

/// <summary>
/// DbContext exclusivo del módulo Facturación.
/// Sólo mapea `Facturas` y `DetallesFactura` (el agregado Factura).
/// NO tiene DbSet&lt;Cliente&gt;, DbSet&lt;Producto&gt; ni DbSet&lt;Usuario&gt;.
/// </summary>
internal sealed class FacturasDbContext(DbContextOptions<FacturasDbContext> options) : DbContext(options)
{
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<DetalleFactura> DetallesFactura => Set<DetalleFactura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Factura>(entity =>
        {
            entity.ToTable("Facturas");
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.Numero, "UX_Factura_Numero").IsUnique();
            entity.Property(f => f.Numero).IsRequired().HasMaxLength(30);
            entity.Property(f => f.ClienteId).IsRequired();
            entity.Property(f => f.FechaEmision).IsRequired();
            entity.Property(f => f.Estado).IsRequired().HasMaxLength(10);
            entity.Property(f => f.Subtotal).HasPrecision(18, 2);
            entity.Property(f => f.Impuestos).HasPrecision(18, 2);
            entity.Property(f => f.Total).HasPrecision(18, 2);

            entity.HasMany(f => f.Detalles)
                .WithOne()
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(f => f.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DetalleFactura>(entity =>
        {
            entity.ToTable("DetallesFactura");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FacturaId).IsRequired();
            entity.Property(d => d.ProductoId).IsRequired();
            entity.Property(d => d.Cantidad).IsRequired();
            entity.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
        });
    }
}

internal sealed class RepositorioFacturasEf(FacturasDbContext db) : IRepositorioFacturas
{
    private static long correlativoEnMemoria;

    public async Task<Factura> CrearConNumeroCorrelativoAsync(
        Func<string, CancellationToken, Task<Factura>> construirFactura,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var siguiente = await db.Database
                .SqlQueryRaw<long>("SELECT NEXT VALUE FOR dbo.NumeroFactura AS [Value]")
                .SingleAsync(cancellationToken);

            var factura = await construirFactura(Factura.FormatearNumero(siguiente), cancellationToken);
            await db.Facturas.AddAsync(factura, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return factura;
        }

        var correlativo = Interlocked.Increment(ref correlativoEnMemoria);
        var facturaMemoria = await construirFactura(Factura.FormatearNumero(correlativo), cancellationToken);
        await db.Facturas.AddAsync(facturaMemoria, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return facturaMemoria;
    }

    public async Task<Factura?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Facturas
            .Include(f => f.Detalles)
            .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Factura>> ConsultarAsync(
        int? clienteId = null,
        string? estado = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Facturas.AsNoTracking().Include(f => f.Detalles).AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(f => f.ClienteId == clienteId.Value);

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(f => f.Estado == estado);

        return await query
            .OrderByDescending(f => f.FechaEmision)
            .ThenByDescending(f => f.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarPendientesAsync(CancellationToken cancellationToken = default) =>
        db.Facturas.AsNoTracking().CountAsync(f => f.Estado == EstadosFactura.Pendiente, cancellationToken);

    public async Task<IReadOnlyList<ResumenEstadoFacturas>> ObtenerResumenPorEstadoAsync(
        CancellationToken cancellationToken = default)
    {
        var facturas = await db.Facturas
            .AsNoTracking()
            .Select(f => new { f.Estado, f.Total })
            .ToListAsync(cancellationToken);

        return facturas
            .GroupBy(f => f.Estado)
            .OrderBy(g => g.Key)
            .Select(g => new ResumenEstadoFacturas(g.Key, g.Count(), g.Sum(x => x.Total)))
            .ToList();
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}

