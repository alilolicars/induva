using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface IDemandRequestService
{
    Task<PagedResult<DemandRequestListItemDto>> SearchAsync(DemandRequestFilter filter, CancellationToken ct = default);
    Task<Result<DemandRequestDetailsDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<SaveDemandRequestDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveDemandRequestDto dto, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveDemandRequestDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> CloseAsync(int id, CancellationToken ct = default);
}

public sealed class DemandRequestService(IUnitOfWork uow, ICurrentUserService user) : IDemandRequestService
{
    public async Task<PagedResult<DemandRequestListItemDto>> SearchAsync(DemandRequestFilter f, CancellationToken ct = default)
    {
        var q = f.Q?.Trim();
        var (items, total) = await uow.Repository<DemandRequest>().PagedAsync(
            x => (f.GovernorateId == null || x.GovernorateId == f.GovernorateId)
                 && (f.Sector == null || x.Sector == f.Sector)
                 && (f.IsExport == null || x.IsExport == f.IsExport)
                 && (string.IsNullOrEmpty(f.Status) || x.Status == f.Status)
                 && (string.IsNullOrEmpty(q) || x.Title.Contains(q) || x.ProductName.Contains(q)),
            f.Page, f.PageSize, ct, x => x.Governorate);

        return new PagedResult<DemandRequestListItemDto>(items.Select(x => x.ToListItem()).ToList(), total, f.Page, f.PageSize);
    }

    public async Task<Result<DemandRequestDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var d = await uow.Repository<DemandRequest>().GetByIdAsync(id, ct, x => x.Governorate);
        if (d is null) return Result<DemandRequestDetailsDto>.Fail(ErrorKind.NotFound, "Demand request not found.");

        return Result<DemandRequestDetailsDto>.Ok(d.ToDetails(CanManage(d)));
    }

    public async Task<Result<SaveDemandRequestDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var d = await uow.Repository<DemandRequest>().GetByIdAsync(id, ct);
        if (d is null) return Result<SaveDemandRequestDto>.Fail(ErrorKind.NotFound, "Demand request not found.");
        if (!CanManage(d)) return Result<SaveDemandRequestDto>.Fail(ErrorKind.Forbidden, "You do not own this demand request.");

        var tags = string.IsNullOrWhiteSpace(d.RequiredTagsCsv)
            ? Array.Empty<string>()
            : d.RequiredTagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Result<SaveDemandRequestDto>.Ok(new SaveDemandRequestDto(
            d.Title,
            d.ProductName,
            d.Sector,
            tags,
            d.Quantity,
            d.Unit,
            d.TargetPriceEgp,
            d.DeadlineUtc,
            d.GovernorateId,
            d.IsExport,
            d.Status,
            d.Description));
    }

    public async Task<Result<int>> CreateAsync(SaveDemandRequestDto dto, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<int>.Fail(ErrorKind.Validation, "Title is required.");

        if (string.IsNullOrWhiteSpace(dto.ProductName))
            return Result<int>.Fail(ErrorKind.Validation, "Product name is required.");

        if (dto.Quantity <= 0)
            return Result<int>.Fail(ErrorKind.Validation, "Quantity must be greater than zero.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown governorate.");

        var normalizedTags = dto.RequiredTags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => CapabilityTags.IsValid(t))
            .Distinct();

        var entity = new DemandRequest
        {
            RequesterUserId = user.UserId,
            Title = dto.Title.Trim(),
            ProductName = dto.ProductName.Trim(),
            Sector = dto.Sector,
            RequiredTagsCsv = string.Join(',', normalizedTags),
            Quantity = dto.Quantity,
            Unit = dto.Unit?.Trim() ?? "units",
            TargetPriceEgp = dto.TargetPriceEgp,
            DeadlineUtc = dto.DeadlineUtc,
            GovernorateId = gov.Id,
            IsExport = dto.IsExport,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status.Trim(),
            Description = dto.Description?.Trim()
        };

        await uow.Repository<DemandRequest>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveDemandRequestDto dto, CancellationToken ct = default)
    {
        var d = await uow.Repository<DemandRequest>().GetByIdAsync(id, ct);
        if (d is null) return Result.Fail(ErrorKind.NotFound, "Demand request not found.");
        if (!CanManage(d)) return Result.Fail(ErrorKind.Forbidden, "You do not own this demand request.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result.Fail(ErrorKind.Validation, "Title is required.");

        if (string.IsNullOrWhiteSpace(dto.ProductName))
            return Result.Fail(ErrorKind.Validation, "Product name is required.");

        if (dto.Quantity <= 0)
            return Result.Fail(ErrorKind.Validation, "Quantity must be greater than zero.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result.Fail(ErrorKind.Validation, "Unknown governorate.");

        var normalizedTags = dto.RequiredTags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => CapabilityTags.IsValid(t))
            .Distinct();

        d.Title = dto.Title.Trim();
        d.ProductName = dto.ProductName.Trim();
        d.Sector = dto.Sector;
        d.RequiredTagsCsv = string.Join(',', normalizedTags);
        d.Quantity = dto.Quantity;
        d.Unit = dto.Unit?.Trim() ?? "units";
        d.TargetPriceEgp = dto.TargetPriceEgp;
        d.DeadlineUtc = dto.DeadlineUtc;
        d.GovernorateId = gov.Id;
        d.IsExport = dto.IsExport;
        d.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status.Trim();
        d.Description = dto.Description?.Trim();

        uow.Repository<DemandRequest>().Update(d);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var d = await uow.Repository<DemandRequest>().GetByIdAsync(id, ct);
        if (d is null) return Result.Fail(ErrorKind.NotFound, "Demand request not found.");
        if (!CanManage(d)) return Result.Fail(ErrorKind.Forbidden, "You do not own this demand request.");

        uow.Repository<DemandRequest>().Remove(d);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> CloseAsync(int id, CancellationToken ct = default)
    {
        var d = await uow.Repository<DemandRequest>().GetByIdAsync(id, ct);
        if (d is null) return Result.Fail(ErrorKind.NotFound, "Demand request not found.");
        if (!CanManage(d)) return Result.Fail(ErrorKind.Forbidden, "You do not own this demand request.");

        d.Status = "Closed";
        uow.Repository<DemandRequest>().Update(d);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private bool CanManage(DemandRequest d) =>
        user.IsInRole(Roles.Admin) || (!string.IsNullOrWhiteSpace(user.UserId) && d.RequesterUserId == user.UserId);
}

