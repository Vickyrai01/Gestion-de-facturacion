using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected Repository(FacturacionDbContext context)
    {
        Context = context;
        Entities = context.Set<TEntity>();
    }

    protected FacturacionDbContext Context { get; }
    protected DbSet<TEntity> Entities { get; }

    public virtual async Task<TEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
        => await Entities.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await Entities.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Entities.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Update(entity);
    }

    public virtual void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Remove(entity);
    }
}
