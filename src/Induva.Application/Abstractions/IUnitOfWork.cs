using Induva.Domain.Common;

namespace Induva.Application.Abstractions;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

