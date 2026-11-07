using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface ISupplierService
{
    Task<PagedResult<SupplierListItemDto>> SearchAsync(SupplierFilter filter, CancellationToken ct = default);
    Task<Result<SupplierDetailsDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<SaveSupplierDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveSupplierDto dto, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveSupplierDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result<int>> AddMaterialAsync(int supplierId, SaveSupplierMaterialDto dto, CancellationToken ct = default);
    Task<Result> RemoveMaterialAsync(int supplierId, int materialId, CancellationToken ct = default);
}

public sealed class SupplierService(IUnitOfWork uow, ICurrentUserService user) : ISupplierService
{
    public async Task<PagedResult<SupplierListItemDto>> SearchAsync(SupplierFilter f, CancellationToken ct = default)
    {
        var q = f.Q?.Trim();
        var (items, total) = await uow.Repository<Supplier>().PagedAsync(
            x => x.IsPublic
                 && (f.GovernorateId == null || x.GovernorateId == f.GovernorateId)
                 && (f.RawMaterialId == null || x.Materials.Any(m => m.RawMaterialId == f.RawMaterialId))
                 && (string.IsNullOrEmpty(q) || x.CompanyName.Contains(q)),
            f.Page, f.PageSize, ct, x => x.Governorate, x => x.Materials);

        return new PagedResult<SupplierListItemDto>(items.Select(x => x.ToListItem()).ToList(), total, f.Page, f.PageSize);
    }

    public async Task<Result<SupplierDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var s = await uow.Repository<Supplier>().GetByIdAsync(id, ct, x => x.Governorate, x => x.Materials);
        if (s is null) return Result<SupplierDetailsDto>.Fail(ErrorKind.NotFound, "Supplier not found.");
        if (!s.IsPublic && !CanManage(s)) return Result<SupplierDetailsDto>.Fail(ErrorKind.Forbidden, "This supplier profile is private.");

        // Load raw material navigation
        var rawMaterials = (await uow.Repository<RawMaterial>().ListAsync(null, ct)).ToDictionary(m => m.Id);

        var materials = s.Materials.Select(m => new SupplierMaterialDto(
            m.Id,
            m.RawMaterialId,
            rawMaterials.TryGetValue(m.RawMaterialId, out var rm) ? rm.NameEn : "Unknown",
            rawMaterials.TryGetValue(m.RawMaterialId, out var rmCat) ? rmCat.Category : "",
            m.MinOrderTons,
            m.PriceNote)).ToList();

        var details = new SupplierDetailsDto(
            s.Id,
            s.CompanyName,
            s.GovernorateId,
            s.Governorate?.NameEn ?? "",
            s.Description,
            s.Latitude,
            s.Longitude,
            s.IsPublic,
            CanManage(s),
            s.OwnerUserId,
            materials);

        return Result<SupplierDetailsDto>.Ok(details);
    }

    public async Task<Result<SaveSupplierDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var s = await uow.Repository<Supplier>().GetByIdAsync(id, ct);
        if (s is null) return Result<SaveSupplierDto>.Fail(ErrorKind.NotFound, "Supplier not found.");
        if (!CanManage(s)) return Result<SaveSupplierDto>.Fail(ErrorKind.Forbidden, "You do not own this supplier profile.");

        return Result<SaveSupplierDto>.Ok(new SaveSupplierDto(
            s.CompanyName,
            s.GovernorateId,
            s.Description,
            s.Latitude,
            s.Longitude,
            s.IsPublic));
    }

    public async Task<Result<int>> CreateAsync(SaveSupplierDto dto, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return Result<int>.Fail(ErrorKind.Validation, "Company name is required.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown governorate.");

        var entity = new Supplier
        {
            OwnerUserId = user.UserId,
            CompanyName = dto.CompanyName.Trim(),
            GovernorateId = gov.Id,
            Description = dto.Description?.Trim(),
            Latitude = dto.Latitude ?? gov.Latitude,
            Longitude = dto.Longitude ?? gov.Longitude,
            IsPublic = dto.IsPublic
        };

        await uow.Repository<Supplier>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveSupplierDto dto, CancellationToken ct = default)
    {
        var s = await uow.Repository<Supplier>().GetByIdAsync(id, ct);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Supplier not found.");
        if (!CanManage(s)) return Result.Fail(ErrorKind.Forbidden, "You do not own this supplier profile.");

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return Result.Fail(ErrorKind.Validation, "Company name is required.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result.Fail(ErrorKind.Validation, "Unknown governorate.");

        s.CompanyName = dto.CompanyName.Trim();
        s.GovernorateId = dto.GovernorateId;
        s.Description = dto.Description?.Trim();
        s.IsPublic = dto.IsPublic;
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            s.Latitude = dto.Latitude.Value;
            s.Longitude = dto.Longitude.Value;
        }

        uow.Repository<Supplier>().Update(s);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var s = await uow.Repository<Supplier>().GetByIdAsync(id, ct);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Supplier not found.");
        if (!CanManage(s)) return Result.Fail(ErrorKind.Forbidden, "You do not own this supplier profile.");

        uow.Repository<Supplier>().Remove(s);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<int>> AddMaterialAsync(int supplierId, SaveSupplierMaterialDto dto, CancellationToken ct = default)
    {
        var s = await uow.Repository<Supplier>().GetByIdAsync(supplierId, ct);
        if (s is null) return Result<int>.Fail(ErrorKind.NotFound, "Supplier not found.");
        if (!CanManage(s)) return Result<int>.Fail(ErrorKind.Forbidden, "You do not own this supplier profile.");

        var raw = await uow.Repository<RawMaterial>().GetByIdAsync(dto.RawMaterialId, ct);
        if (raw is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown raw material.");

        var entity = new SupplierMaterial
        {
            SupplierId = supplierId,
            RawMaterialId = dto.RawMaterialId,
            MinOrderTons = dto.MinOrderTons,
            PriceNote = dto.PriceNote?.Trim()
        };

        await uow.Repository<SupplierMaterial>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> RemoveMaterialAsync(int supplierId, int materialId, CancellationToken ct = default)
    {
        var s = await uow.Repository<Supplier>().GetByIdAsync(supplierId, ct);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Supplier not found.");
        if (!CanManage(s)) return Result.Fail(ErrorKind.Forbidden, "You do not own this supplier profile.");

        var mat = await uow.Repository<SupplierMaterial>().GetByIdAsync(materialId, ct);
        if (mat is null || mat.SupplierId != supplierId)
            return Result.Fail(ErrorKind.NotFound, "Supplier material not found.");

        uow.Repository<SupplierMaterial>().Remove(mat);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private bool CanManage(Supplier s) =>
        user.IsInRole(Roles.Admin) || (!string.IsNullOrWhiteSpace(user.UserId) && s.OwnerUserId == user.UserId);
}

