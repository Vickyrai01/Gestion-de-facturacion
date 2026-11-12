namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);
}