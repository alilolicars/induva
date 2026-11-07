using Induva.Application.Abstractions;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Induva.Application.Services;

public interface ILookupService
{
    Task<IReadOnlyList<GovernorateDto>> GetGovernoratesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RawMaterialDto>> GetRawMaterialsAsync(CancellationToken ct = default);
}

public sealed class LookupService(IUnitOfWork uow, IMemoryCache cache) : ILookupService
{
    private const string GovernoratesCacheKey = "lookups_governorates";
    private const string RawMaterialsCacheKey = "lookups_raw_materials";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    public async Task<IReadOnlyList<GovernorateDto>> GetGovernoratesAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(GovernoratesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var list = await uow.Repository<Governorate>().ListAsync(null, ct);
            return list.OrderBy(g => g.NameEn).Select(g => g.ToDto()).ToList() as IReadOnlyList<GovernorateDto>;
        }) ?? [];
    }

    public async Task<IReadOnlyList<RawMaterialDto>> GetRawMaterialsAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(RawMaterialsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var list = await uow.Repository<RawMaterial>().ListAsync(null, ct);
            return list.OrderBy(m => m.NameEn).Select(m => m.ToDto()).ToList() as IReadOnlyList<RawMaterialDto>;
        }) ?? [];
    }
}

