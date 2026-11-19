using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Clientes.Contracts;
using Facturacion.Modules.Clientes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Modules.Clientes.Infrastructure;

/// <summary>
/// DbContext exclusivo del módulo Clientes.
///
/// En el legacy (`FacturacionDbContext`) y en las Clases 4-6 un único DbContext
/// tenía los 5 DbSets y las relaciones cruzadas entre módulos.
/// En el monolito modular, ClientesDbContext sólo conoce la tabla `Clientes`.
/// </summary>
internal sealed class ClientesDbContext(DbContextOptions<ClientesDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Documento).IsRequired().HasMaxLength(30);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(254);
            entity.Property(c => c.Activo).IsRequired();
        });
    }
}

internal sealed class RepositorioClientesEf(ClientesDbContext db) : IRepositorioClientes
{
    public async Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken = default) =>
        await db.Clientes.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Cliente>> ListarActivosAsync(CancellationToken cancellationToken = default) =>
        await db.Clientes.AsNoTracking().Where(c => c.Activo).OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync(cancellationToken);

    public async Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Cliente?> ObtenerPorDocumentoAsync(string documento, CancellationToken cancellationToken = default) =>
        await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Documento == documento, cancellationToken);

    public async Task<bool> ExisteDocumentoAsync(string documento, int exceptoId = 0, CancellationToken cancellationToken = default) =>
        await db.Clientes.AsNoTracking().AnyAsync(c => c.Documento == documento && c.Id != exceptoId, cancellationToken);

    public async Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default) =>
        await db.Clientes.AddAsync(cliente, cancellationToken);

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        PersistenciaModulosExtensions.GuardarManejandoConflictosAsync(
            db,
            "Ya existe un cliente con ese documento.",
            cancellationToken);
}

/// <summary>
/// Implementa el contrato público <see cref="IConsultaClientes"/> que consumen otros módulos
/// (como Facturación) sin exponer la entidad <see cref="Cliente"/> ni el <see cref="ClientesDbContext"/>.
/// </summary>
internal sealed class ConsultaClientesModulo(ClientesDbContext db) : IConsultaClientes
{
    public async Task<ClienteCatalogoDto?> ObtenerPorIdAsync(int clienteId, CancellationToken cancellationToken = default) =>
        await db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => new ClienteCatalogoDto(c.Id, c.Nombre, c.Apellido, c.Documento, c.Email, c.Activo))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> EstaActivoAsync(int clienteId, CancellationToken cancellationToken = default) =>
        await db.Clientes
            .AsNoTracking()
            .AnyAsync(c => c.Id == clienteId && c.Activo, cancellationToken);

    public async Task<IReadOnlyList<ClienteCatalogoDto>> ListarActivosAsync(CancellationToken cancellationToken = default) =>
        await db.Clientes
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .Select(c => new ClienteCatalogoDto(c.Id, c.Nombre, c.Apellido, c.Documento, c.Email, c.Activo))
            .ToListAsync(cancellationToken);
}

