using System.Linq.Expressions;
using Induva.Application.Abstractions;
using Induva.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Induva.Infrastructure.Persistence.Repositories;

public class GenericRepository<T>(ApplicationDbContext db) : IGenericRepository<T> where T : BaseEntity
{
    private IQueryable<T> Build(Expression<Func<T, bool>>? filter, Expression<Func<T, object>>[] includes, bool tracking)
    {
        IQueryable<T> q = db.Set<T>();
        if (!tracking) q = q.AsNoTracking();
        foreach (var inc in includes) q = q.Include(inc);
        if (includes.Length > 1) q = q.AsSplitQuery(); // avoids cartesian explosion with multiple collection includes
        if (filter is not null) q = q.Where(filter);
        return q;
    }

    public Task<T?> GetByIdAsync(int id, CancellationToken ct = default, params Expression<Func<T, object>>[] includes)
        => Build(null, includes, tracking: true).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default,
        params Expression<Func<T, object>>[] includes)
        => await Build(filter, includes, tracking: false).ToListAsync(ct);

    public async Task<(IReadOnlyList<T> Items, int Total)> PagedAsync(Expression<Func<T, bool>>? filter, int page, int pageSize,
        CancellationToken ct = default, params Expression<Func<T, object>>[] includes)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = Build(filter, includes, tracking: false);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(e => e.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
        => (filter is null ? db.Set<T>() : db.Set<T>().Where(filter)).CountAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) => await db.Set<T>().AddAsync(entity, ct);
    public void Update(T entity) => db.Set<T>().Update(entity);
    public void Remove(T entity) => db.Set<T>().Remove(entity);
}

