using InventiCore.Domain.Entities;

namespace InventiCore.Domain.Interfaces;

/// <summary>
/// Interface genérica de repositório. Segue o princípio ISP (Interface Segregation).
/// Cada entidade pode ter seu próprio repositório herdando desta.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
