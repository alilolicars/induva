using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface IStartupService
{
    Task<PagedResult<StartupListItemDto>> SearchAsync(StartupFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<StartupListItemDto>> GetMyStartupsAsync(CancellationToken ct = default);
    Task<Result<StartupDetailsDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<SaveStartupDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveStartupDto dto, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveStartupDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class StartupService(IUnitOfWork uow, ICurrentUserService user) : IStartupService
{
    public async Task<PagedResult<StartupListItemDto>> SearchAsync(StartupFilter f, CancellationToken ct = default)
    {
        var q = f.Q?.Trim();
        var (items, total) = await uow.Repository<Startup>().PagedAsync(
            x => (f.GovernorateId == null || x.GovernorateId == f.GovernorateId)
                 && (f.Sector == null || x.Sector == f.Sector)
                 && (f.Stage == null || x.Stage == f.Stage)
                 && (string.IsNullOrEmpty(q) || x.Name.Contains(q) || x.ProductIdea.Contains(q)),
            f.Page, f.PageSize, ct, x => x.Governorate!);

        return new PagedResult<StartupListItemDto>(items.Select(x => x.ToListItem()).ToList(), total, f.Page, f.PageSize);
    }

    public async Task<IReadOnlyList<StartupListItemDto>> GetMyStartupsAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) return [];
        var list = await uow.Repository<Startup>().ListAsync(
            s => s.OwnerUserId == user.UserId,
            ct,
            s => s.Governorate!);
        return list.Select(s => s.ToListItem()).ToList();
    }

    public async Task<Result<StartupDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var s = await uow.Repository<Startup>().GetByIdAsync(id, ct, x => x.Governorate!);
        if (s is null) return Result<StartupDetailsDto>.Fail(ErrorKind.NotFound, "Startup profile not found.");

        return Result<StartupDetailsDto>.Ok(s.ToDetails(CanManage(s)));
    }

    public async Task<Result<SaveStartupDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var s = await uow.Repository<Startup>().GetByIdAsync(id, ct);
        if (s is null) return Result<SaveStartupDto>.Fail(ErrorKind.NotFound, "Startup profile not found.");
        if (!CanManage(s)) return Result<SaveStartupDto>.Fail(ErrorKind.Forbidden, "You do not own this startup profile.");

        var tags = string.IsNullOrWhiteSpace(s.RequiredTagsCsv)
            ? Array.Empty<string>()
            : s.RequiredTagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Result<SaveStartupDto>.Ok(new SaveStartupDto(
            s.Name,
            s.Sector,
            s.Stage,
            s.Description,
            s.ProductIdea,
            s.FundingNeededEgp,
            tags,
            s.GovernorateId));
    }

    public async Task<Result<int>> CreateAsync(SaveStartupDto dto, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<int>.Fail(ErrorKind.Validation, "Startup name is required.");

        if (string.IsNullOrWhiteSpace(dto.ProductIdea))
            return Result<int>.Fail(ErrorKind.Validation, "Product idea is required.");

        if (dto.GovernorateId.HasValue)
        {
            var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId.Value, ct);
            if (gov is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown governorate.");
        }

        var normalizedTags = dto.RequiredTags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => CapabilityTags.IsValid(t))
            .Distinct();

        var entity = new Startup
        {
            OwnerUserId = user.UserId,
            Name = dto.Name.Trim(),
            Sector = dto.Sector,
            Stage = dto.Stage,
            Description = dto.Description?.Trim(),
            ProductIdea = dto.ProductIdea.Trim(),
            FundingNeededEgp = dto.FundingNeededEgp,
            RequiredTagsCsv = string.Join(',', normalizedTags),
            GovernorateId = dto.GovernorateId
        };

        await uow.Repository<Startup>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveStartupDto dto, CancellationToken ct = default)
    {
        var s = await uow.Repository<Startup>().GetByIdAsync(id, ct);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Startup profile not found.");
        if (!CanManage(s)) return Result.Fail(ErrorKind.Forbidden, "You do not own this startup profile.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result.Fail(ErrorKind.Validation, "Startup name is required.");

        if (string.IsNullOrWhiteSpace(dto.ProductIdea))
            return Result.Fail(ErrorKind.Validation, "Product idea is required.");

        if (dto.GovernorateId.HasValue)
        {
            var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId.Value, ct);
            if (gov is null) return Result.Fail(ErrorKind.Validation, "Unknown governorate.");
        }

        var normalizedTags = dto.RequiredTags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => CapabilityTags.IsValid(t))
            .Distinct();

        s.Name = dto.Name.Trim();
        s.Sector = dto.Sector;
        s.Stage = dto.Stage;
        s.Description = dto.Description?.Trim();
        s.ProductIdea = dto.ProductIdea.Trim();
        s.FundingNeededEgp = dto.FundingNeededEgp;
        s.RequiredTagsCsv = string.Join(',', normalizedTags);
        s.GovernorateId = dto.GovernorateId;

        uow.Repository<Startup>().Update(s);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var s = await uow.Repository<Startup>().GetByIdAsync(id, ct);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Startup profile not found.");
        if (!CanManage(s)) return Result.Fail(ErrorKind.Forbidden, "You do not own this startup profile.");

        uow.Repository<Startup>().Remove(s);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private bool CanManage(Startup s) =>
        user.IsInRole(Roles.Admin) || (!string.IsNullOrWhiteSpace(user.UserId) && s.OwnerUserId == user.UserId);
}

