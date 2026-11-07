using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface IInvestorService
{
    Task<PagedResult<InvestorListItemDto>> SearchAsync(InvestorFilter filter, CancellationToken ct = default);
    Task<Result<InvestorDetailsDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<InvestorDetailsDto>> GetCurrentAsync(CancellationToken ct = default);
    Task<Result<SaveInvestorProfileDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveAsync(SaveInvestorProfileDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class InvestorService(IUnitOfWork uow, ICurrentUserService user) : IInvestorService
{
    public async Task<PagedResult<InvestorListItemDto>> SearchAsync(InvestorFilter f, CancellationToken ct = default)
    {
        var q = f.Q?.Trim();
        var sectorStr = f.Sector?.ToString();
        var stageStr = f.Stage?.ToString();

        var (items, total) = await uow.Repository<InvestorProfile>().PagedAsync(
            x => (sectorStr == null || x.SectorsCsv.Contains(sectorStr))
                 && (stageStr == null || x.PreferredStagesCsv.Contains(stageStr))
                 && (string.IsNullOrEmpty(q) || x.DisplayName.Contains(q) || (x.Bio != null && x.Bio.Contains(q))),
            f.Page, f.PageSize, ct);

        return new PagedResult<InvestorListItemDto>(items.Select(x => x.ToListItem()).ToList(), total, f.Page, f.PageSize);
    }

    public async Task<Result<InvestorDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var i = await uow.Repository<InvestorProfile>().GetByIdAsync(id, ct);
        if (i is null) return Result<InvestorDetailsDto>.Fail(ErrorKind.NotFound, "Investor profile not found.");

        return Result<InvestorDetailsDto>.Ok(i.ToDetails(CanManage(i)));
    }

    public async Task<Result<InvestorDetailsDto>> GetCurrentAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<InvestorDetailsDto>.Fail(ErrorKind.Forbidden, "Please log in.");

        var list = await uow.Repository<InvestorProfile>().ListAsync(x => x.OwnerUserId == user.UserId, ct);
        var i = list.FirstOrDefault();
        if (i is null) return Result<InvestorDetailsDto>.Fail(ErrorKind.NotFound, "No investor profile found.");

        return Result<InvestorDetailsDto>.Ok(i.ToDetails(isOwner: true));
    }

    public async Task<Result<SaveInvestorProfileDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var i = await uow.Repository<InvestorProfile>().GetByIdAsync(id, ct);
        if (i is null) return Result<SaveInvestorProfileDto>.Fail(ErrorKind.NotFound, "Investor profile not found.");
        if (!CanManage(i)) return Result<SaveInvestorProfileDto>.Fail(ErrorKind.Forbidden, "You do not own this investor profile.");

        var sectors = string.IsNullOrWhiteSpace(i.SectorsCsv)
            ? []
            : i.SectorsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<Induva.Domain.Enums.Sector>(s, out var sec) ? sec : (Induva.Domain.Enums.Sector?)null)
                .Where(s => s.HasValue).Select(s => s!.Value).ToList();

        var stages = string.IsNullOrWhiteSpace(i.PreferredStagesCsv)
            ? []
            : i.PreferredStagesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<Induva.Domain.Enums.StartupStage>(s, out var stg) ? stg : (Induva.Domain.Enums.StartupStage?)null)
                .Where(s => s.HasValue).Select(s => s!.Value).ToList();

        return Result<SaveInvestorProfileDto>.Ok(new SaveInvestorProfileDto(
            i.DisplayName,
            sectors,
            i.MinTicketEgp,
            i.MaxTicketEgp,
            stages,
            i.Bio));
    }

    public async Task<Result<int>> SaveAsync(SaveInvestorProfileDto dto, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
            return Result<int>.Fail(ErrorKind.Validation, "Display name is required.");

        var existingSummary = (await uow.Repository<InvestorProfile>().ListAsync(x => x.OwnerUserId == user.UserId, ct)).FirstOrDefault();

        var sectorsCsv = string.Join(',', dto.Sectors.Select(s => s.ToString()));
        var stagesCsv = string.Join(',', dto.PreferredStages.Select(s => s.ToString()));

        if (existingSummary is null)
        {
            var entity = new InvestorProfile
            {
                OwnerUserId = user.UserId,
                DisplayName = dto.DisplayName.Trim(),
                SectorsCsv = sectorsCsv,
                MinTicketEgp = dto.MinTicketEgp,
                MaxTicketEgp = dto.MaxTicketEgp,
                PreferredStagesCsv = stagesCsv,
                Bio = dto.Bio?.Trim()
            };
            await uow.Repository<InvestorProfile>().AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            return Result<int>.Ok(entity.Id);
        }
        else
        {
            var existing = await uow.Repository<InvestorProfile>().GetByIdAsync(existingSummary.Id, ct) ?? existingSummary;
            existing.DisplayName = dto.DisplayName.Trim();
            existing.SectorsCsv = sectorsCsv;
            existing.MinTicketEgp = dto.MinTicketEgp;
            existing.MaxTicketEgp = dto.MaxTicketEgp;
            existing.PreferredStagesCsv = stagesCsv;
            existing.Bio = dto.Bio?.Trim();

            uow.Repository<InvestorProfile>().Update(existing);
            await uow.SaveChangesAsync(ct);
            return Result<int>.Ok(existing.Id);
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var i = await uow.Repository<InvestorProfile>().GetByIdAsync(id, ct);
        if (i is null) return Result.Fail(ErrorKind.NotFound, "Investor profile not found.");
        if (!CanManage(i)) return Result.Fail(ErrorKind.Forbidden, "You do not own this investor profile.");

        uow.Repository<InvestorProfile>().Remove(i);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private bool CanManage(InvestorProfile i) =>
        user.IsInRole(Roles.Admin) || (!string.IsNullOrWhiteSpace(user.UserId) && i.OwnerUserId == user.UserId);
}
