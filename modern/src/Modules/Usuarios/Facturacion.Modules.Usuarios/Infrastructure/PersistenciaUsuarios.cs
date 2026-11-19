using Facturacion.BuildingBlocks.Infrastructure.Persistencia;
using Facturacion.Modules.Usuarios.Domain;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Modules.Usuarios.Infrastructure;

/// <summary>
/// DbContext exclusivo del módulo Usuarios.
/// Sólo mapea la tabla `Usuarios` y su índice único `UX_Usuario_Nombre`.
/// </summary>
internal sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.NombreUsuario, "UX_Usuario_Nombre").IsUnique();
            entity.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Rol).IsRequired().HasMaxLength(10);
            entity.Property(u => u.Activo).IsRequired();
        });
    }
}

internal sealed class RepositorioUsuariosEf(UsuariosDbContext db) : IRepositorioUsuarios
{
    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
        await db.Usuarios.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);

    public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<Usuario?> ObtenerPorNombreAsync(string nombreNormalizado, CancellationToken cancellationToken = default) =>
        await db.Usuarios.SingleOrDefaultAsync(u => u.NombreUsuario == nombreNormalizado, cancellationToken);

    public async Task<bool> ExisteNombreAsync(string nombreNormalizado, int exceptoId = 0, CancellationToken cancellationToken = default) =>
        await db.Usuarios.AsNoTracking().AnyAsync(u => u.NombreUsuario == nombreNormalizado && u.Id != exceptoId, cancellationToken);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        await db.Usuarios.AddAsync(usuario, cancellationToken);

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        PersistenciaModulosExtensions.GuardarManejandoConflictosAsync(
            db,
            "Ya existe un usuario con ese nombre.",
            cancellationToken);
}

