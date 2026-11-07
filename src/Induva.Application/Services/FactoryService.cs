using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface IFactoryService
{
    Task<PagedResult<FactoryListItemDto>> SearchAsync(FactoryFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<FactoryListItemDto>> GetMyFactoriesAsync(CancellationToken ct = default);
    Task<Result<FactoryDetailsDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<SaveFactoryDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveFactoryDto dto, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveFactoryDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    // Child management
    Task<Result<int>> AddMachineAsync(int factoryId, SaveMachineDto dto, CancellationToken ct = default);
    Task<Result> RemoveMachineAsync(int factoryId, int machineId, CancellationToken ct = default);

    Task<Result<int>> AddIdleWindowAsync(int factoryId, SaveIdleWindowDto dto, CancellationToken ct = default);
    Task<Result> RemoveIdleWindowAsync(int factoryId, int windowId, CancellationToken ct = default);

    Task<Result<int>> AddProductAsync(int factoryId, SaveFactoryProductDto dto, CancellationToken ct = default);
    Task<Result> RemoveProductAsync(int factoryId, int productId, CancellationToken ct = default);

    Task<Result<int>> AddMaterialNeedAsync(int factoryId, SaveFactoryMaterialNeedDto dto, CancellationToken ct = default);
    Task<Result> RemoveMaterialNeedAsync(int factoryId, int needId, CancellationToken ct = default);
}

public sealed class FactoryService(IUnitOfWork uow, ICurrentUserService user) : IFactoryService
{
    public async Task<PagedResult<FactoryListItemDto>> SearchAsync(FactoryFilter f, CancellationToken ct = default)
    {
        var q = f.Q?.Trim();
        var (items, total) = await uow.Repository<Factory>().PagedAsync(
            x => x.IsPublic
                 && (f.GovernorateId == null || x.GovernorateId == f.GovernorateId)
                 && (f.Sector == null || x.Sector == f.Sector)
                 && (string.IsNullOrEmpty(q) || x.Name.Contains(q)),
            f.Page, f.PageSize, ct, x => x.Governorate, x => x.Machines);

        return new PagedResult<FactoryListItemDto>(items.Select(x => x.ToListItem()).ToList(), total, f.Page, f.PageSize);
    }

    public async Task<IReadOnlyList<FactoryListItemDto>> GetMyFactoriesAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) return [];
        var list = await uow.Repository<Factory>().ListAsync(
            f => f.OwnerUserId == user.UserId,
            ct,
            f => f.Governorate!,
            f => f.Machines);
        return list.Select(f => f.ToListItem()).ToList();
    }

    public async Task<Result<FactoryDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct,
            x => x.Governorate,
            x => x.Machines,
            x => x.IdleWindows,
            x => x.Products,
            x => x.MaterialNeeds,
            x => x.CapabilityProfile!);

        if (f is null) return Result<FactoryDetailsDto>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!f.IsPublic && !CanManage(f)) return Result<FactoryDetailsDto>.Fail(ErrorKind.Forbidden, "This profile is private.");

        // For MaterialNeeds, we also need RawMaterial navigation loaded if available
        var rawMaterials = await uow.Repository<RawMaterial>().ListAsync(null, ct);
        var rawMaterialMap = rawMaterials.ToDictionary(m => m.Id, m => m.NameEn);

        var tags = string.IsNullOrWhiteSpace(f.CapabilityProfile?.TagsCsv)
            ? Array.Empty<string>()
            : f.CapabilityProfile!.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var materialNeeds = f.MaterialNeeds.Select(n => new FactoryMaterialNeedDto(
            n.Id,
            n.RawMaterialId,
            rawMaterialMap.TryGetValue(n.RawMaterialId, out var name) ? name : "Unknown Material",
            n.MonthlyTons)).ToList();

        var details = new FactoryDetailsDto(
            f.Id,
            f.Name,
            f.Sector,
            f.GovernorateId,
            f.Governorate?.NameEn ?? "",
            f.Description,
            f.UtilizationPercent,
            f.Latitude,
            f.Longitude,
            f.IsPublic,
            CanManage(f),
            f.OwnerUserId,
            f.Machines.Select(m => m.ToDto()).ToList(),
            f.IdleWindows.Select(w => w.ToDto()).ToList(),
            f.Products.Select(p => p.ToDto()).ToList(),
            materialNeeds,
            tags,
            f.CapabilityProfile?.Summary);

        return Result<FactoryDetailsDto>.Ok(details);
    }

    public async Task<Result<SaveFactoryDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct);
        if (f is null) return Result<SaveFactoryDto>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result<SaveFactoryDto>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        return Result<SaveFactoryDto>.Ok(new SaveFactoryDto(
            f.Name,
            f.Sector,
            f.GovernorateId,
            f.Description,
            f.UtilizationPercent,
            f.Latitude,
            f.Longitude,
            f.IsPublic));
    }

    public async Task<Result<int>> CreateAsync(SaveFactoryDto dto, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<int>.Fail(ErrorKind.Validation, "Factory name is required.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown governorate.");

        var entity = new Factory
        {
            OwnerUserId = user.UserId,
            Name = dto.Name.Trim(),
            Sector = dto.Sector,
            GovernorateId = gov.Id,
            Description = dto.Description?.Trim(),
            UtilizationPercent = Math.Clamp(dto.UtilizationPercent, 0, 100),
            Latitude = dto.Latitude ?? gov.Latitude,
            Longitude = dto.Longitude ?? gov.Longitude,
            IsPublic = dto.IsPublic
        };

        await uow.Repository<Factory>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveFactoryDto dto, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result.Fail(ErrorKind.Validation, "Factory name is required.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result.Fail(ErrorKind.Validation, "Unknown governorate.");

        f.Name = dto.Name.Trim();
        f.Sector = dto.Sector;
        f.GovernorateId = dto.GovernorateId;
        f.Description = dto.Description?.Trim();
        f.UtilizationPercent = Math.Clamp(dto.UtilizationPercent, 0, 100);
        f.IsPublic = dto.IsPublic;
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            f.Latitude = dto.Latitude.Value;
            f.Longitude = dto.Longitude.Value;
        }

        uow.Repository<Factory>().Update(f);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        uow.Repository<Factory>().Remove(f);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // --- Machines ---

    public async Task<Result<int>> AddMachineAsync(int factoryId, SaveMachineDto dto, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result<int>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result<int>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<int>.Fail(ErrorKind.Validation, "Machine name is required.");

        var machine = new Machine
        {
            FactoryId = factoryId,
            Name = dto.Name.Trim(),
            MachineType = dto.MachineType?.Trim(),
            Quantity = Math.Max(1, dto.Quantity),
            CapacityPerHour = dto.CapacityPerHour,
            CapacityUnit = dto.CapacityUnit?.Trim(),
            Description = dto.Description?.Trim()
        };

        await uow.Repository<Machine>().AddAsync(machine, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(machine.Id);
    }

    public async Task<Result> RemoveMachineAsync(int factoryId, int machineId, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        var machine = await uow.Repository<Machine>().GetByIdAsync(machineId, ct);
        if (machine is null || machine.FactoryId != factoryId)
            return Result.Fail(ErrorKind.NotFound, "Machine not found.");

        uow.Repository<Machine>().Remove(machine);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // --- Idle Windows ---

    public async Task<Result<int>> AddIdleWindowAsync(int factoryId, SaveIdleWindowDto dto, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result<int>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result<int>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        if (dto.StartMonth < 1 || dto.StartMonth > 12 || dto.EndMonth < 1 || dto.EndMonth > 12)
            return Result<int>.Fail(ErrorKind.Validation, "Months must be between 1 and 12.");

        var window = new IdleWindow
        {
            FactoryId = factoryId,
            StartMonth = dto.StartMonth,
            EndMonth = dto.EndMonth,
            AvailableCapacityPercent = Math.Clamp(dto.AvailableCapacityPercent, 0, 100),
            Note = dto.Note?.Trim()
        };

        await uow.Repository<IdleWindow>().AddAsync(window, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(window.Id);
    }

    public async Task<Result> RemoveIdleWindowAsync(int factoryId, int windowId, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        var window = await uow.Repository<IdleWindow>().GetByIdAsync(windowId, ct);
        if (window is null || window.FactoryId != factoryId)
            return Result.Fail(ErrorKind.NotFound, "Idle window not found.");

        uow.Repository<IdleWindow>().Remove(window);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // --- Products ---

    public async Task<Result<int>> AddProductAsync(int factoryId, SaveFactoryProductDto dto, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result<int>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result<int>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<int>.Fail(ErrorKind.Validation, "Product name is required.");

        var prod = new FactoryProduct
        {
            FactoryId = factoryId,
            Name = dto.Name.Trim(),
            Category = dto.Category?.Trim()
        };

        await uow.Repository<FactoryProduct>().AddAsync(prod, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(prod.Id);
    }

    public async Task<Result> RemoveProductAsync(int factoryId, int productId, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        var prod = await uow.Repository<FactoryProduct>().GetByIdAsync(productId, ct);
        if (prod is null || prod.FactoryId != factoryId)
            return Result.Fail(ErrorKind.NotFound, "Product not found.");

        uow.Repository<FactoryProduct>().Remove(prod);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // --- Material Needs ---

    public async Task<Result<int>> AddMaterialNeedAsync(int factoryId, SaveFactoryMaterialNeedDto dto, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result<int>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result<int>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        var mat = await uow.Repository<RawMaterial>().GetByIdAsync(dto.RawMaterialId, ct);
        if (mat is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown raw material.");

        var need = new FactoryMaterialNeed
        {
            FactoryId = factoryId,
            RawMaterialId = dto.RawMaterialId,
            MonthlyTons = dto.MonthlyTons
        };

        await uow.Repository<FactoryMaterialNeed>().AddAsync(need, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(need.Id);
    }

    public async Task<Result> RemoveMaterialNeedAsync(int factoryId, int needId, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(factoryId, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        var need = await uow.Repository<FactoryMaterialNeed>().GetByIdAsync(needId, ct);
        if (need is null || need.FactoryId != factoryId)
            return Result.Fail(ErrorKind.NotFound, "Material need not found.");

        uow.Repository<FactoryMaterialNeed>().Remove(need);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private bool CanManage(Factory f) =>
        user.IsInRole(Roles.Admin) || (!string.IsNullOrWhiteSpace(user.UserId) && f.OwnerUserId == user.UserId);
}

