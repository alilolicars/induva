using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record FactoryFilter(int? GovernorateId = null, Sector? Sector = null, string? Q = null, int Page = 1, int PageSize = 12);

public sealed record FactoryListItemDto(int Id, string Name, Sector Sector, string GovernorateName, int UtilizationPercent, int MachineCount);

public sealed record MachineDto(int Id, string Name, string? MachineType, int Quantity, decimal? CapacityPerHour, string? CapacityUnit, string? Description = null);

public sealed record IdleWindowDto(int Id, int StartMonth, int EndMonth, int AvailableCapacityPercent, string? Note);

public sealed record FactoryProductDto(int Id, string Name, string? Category);

public sealed record FactoryMaterialNeedDto(int Id, int RawMaterialId, string RawMaterialName, decimal? MonthlyTons);

public sealed record FactoryDetailsDto(
    int Id,
    string Name,
    Sector Sector,
    int GovernorateId,
    string GovernorateName,
    string? Description,
    int UtilizationPercent,
    double Latitude,
    double Longitude,
    bool IsPublic,
    bool IsOwner,
    string OwnerUserId,
    IReadOnlyList<MachineDto> Machines,
    IReadOnlyList<IdleWindowDto> IdleWindows,
    IReadOnlyList<FactoryProductDto> Products,
    IReadOnlyList<FactoryMaterialNeedDto> MaterialNeeds,
    IReadOnlyList<string> CapabilityTags,
    string? CapabilitySummary = null);

public sealed record SaveFactoryDto(
    string Name,
    Sector Sector,
    int GovernorateId,
    string? Description,
    int UtilizationPercent,
    double? Latitude,
    double? Longitude,
    bool IsPublic);

public sealed record SaveMachineDto(
    string Name,
    string? MachineType,
    int Quantity,
    decimal? CapacityPerHour,
    string? CapacityUnit,
    string? Description);

public sealed record SaveIdleWindowDto(
    int StartMonth,
    int EndMonth,
    int AvailableCapacityPercent,
    string? Note);

public sealed record SaveFactoryProductDto(
    string Name,
    string? Category);

public sealed record SaveFactoryMaterialNeedDto(
    int RawMaterialId,
    decimal? MonthlyTons);

