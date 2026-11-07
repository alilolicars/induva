using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface IGovernorateResourceService
{
    Task<IReadOnlyList<GovernorateResourceDto>> ListAsync(int? governorateId = null, CancellationToken ct = default);
    Task<Result<GovernorateResourceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveGovernorateResourceDto dto, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveGovernorateResourceDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class GovernorateResourceService(IUnitOfWork uow, ICurrentUserService user) : IGovernorateResourceService
{
    public async Task<IReadOnlyList<GovernorateResourceDto>> ListAsync(int? governorateId = null, CancellationToken ct = default)
    {
        var list = await uow.Repository<GovernorateResource>().ListAsync(
            r => governorateId == null || r.GovernorateId == governorateId,
            ct,
            r => r.Governorate,
            r => r.RawMaterial);

        return list.Select(r => new GovernorateResourceDto(
            r.Id,
            r.GovernorateId,
            r.Governorate?.NameEn ?? "",
            r.RawMaterialId,
            r.RawMaterial?.NameEn ?? "",
            r.RawMaterial?.Category ?? "",
            r.EstimatedAnnualTons,
            r.Notes)).ToList();
    }

    public async Task<Result<GovernorateResourceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var r = await uow.Repository<GovernorateResource>().GetByIdAsync(id, ct, x => x.Governorate, x => x.RawMaterial);
        if (r is null) return Result<GovernorateResourceDto>.Fail(ErrorKind.NotFound, "Resource not found.");

        return Result<GovernorateResourceDto>.Ok(new GovernorateResourceDto(
            r.Id,
            r.GovernorateId,
            r.Governorate?.NameEn ?? "",
            r.RawMaterialId,
            r.RawMaterial?.NameEn ?? "",
            r.RawMaterial?.Category ?? "",
            r.EstimatedAnnualTons,
            r.Notes));
    }

    public async Task<Result<int>> CreateAsync(SaveGovernorateResourceDto dto, CancellationToken ct = default)
    {
        if (!user.IsInRole(Roles.Admin)) return Result<int>.Fail(ErrorKind.Forbidden, "Admin permission required.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown governorate.");

        var mat = await uow.Repository<RawMaterial>().GetByIdAsync(dto.RawMaterialId, ct);
        if (mat is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown raw material.");

        var entity = new GovernorateResource
        {
            GovernorateId = dto.GovernorateId,
            RawMaterialId = dto.RawMaterialId,
            EstimatedAnnualTons = dto.EstimatedAnnualTons,
            Notes = dto.Notes?.Trim()
        };

        await uow.Repository<GovernorateResource>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveGovernorateResourceDto dto, CancellationToken ct = default)
    {
        if (!user.IsInRole(Roles.Admin)) return Result.Fail(ErrorKind.Forbidden, "Admin permission required.");

        var entity = await uow.Repository<GovernorateResource>().GetByIdAsync(id, ct);
        if (entity is null) return Result.Fail(ErrorKind.NotFound, "Resource not found.");

        entity.GovernorateId = dto.GovernorateId;
        entity.RawMaterialId = dto.RawMaterialId;
        entity.EstimatedAnnualTons = dto.EstimatedAnnualTons;
        entity.Notes = dto.Notes?.Trim();

        uow.Repository<GovernorateResource>().Update(entity);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!user.IsInRole(Roles.Admin)) return Result.Fail(ErrorKind.Forbidden, "Admin permission required.");

        var entity = await uow.Repository<GovernorateResource>().GetByIdAsync(id, ct);
        if (entity is null) return Result.Fail(ErrorKind.NotFound, "Resource not found.");

        uow.Repository<GovernorateResource>().Remove(entity);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

