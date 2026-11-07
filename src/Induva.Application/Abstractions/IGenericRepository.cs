using System.Linq.Expressions;
using Induva.Domain.Common;

namespace Induva.Application.Abstractions;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default, params Expression<Func<T, object>>[] includes);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default,
                                     params Expression<Func<T, object>>[] includes);
    Task<(IReadOnlyList<T> Items, int Total)> PagedAsync(Expression<Func<T, bool>>? filter, int page, int pageSize,
                                     CancellationToken ct = default, params Expression<Func<T, object>>[] includes);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

