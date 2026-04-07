namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IBaseRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
