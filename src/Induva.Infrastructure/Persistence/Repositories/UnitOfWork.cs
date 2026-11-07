using Induva.Application.Abstractions;
using Induva.Domain.Common;

namespace Induva.Infrastructure.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext db) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repos = new();

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        if (!_repos.TryGetValue(typeof(T), out var repo))
            _repos[typeof(T)] = repo = new GenericRepository<T>(db);
        return (IGenericRepository<T>)repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

