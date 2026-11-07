namespace Induva.Application.Dtos;

public sealed record SupplierFilter(int? GovernorateId = null, int? RawMaterialId = null, string? Q = null, int Page = 1, int PageSize = 12);

public sealed record SupplierListItemDto(int Id, string CompanyName, string GovernorateName, int MaterialCount);

public sealed record SupplierMaterialDto(int Id, int RawMaterialId, string RawMaterialName, string Category, decimal? MinOrderTons, string? PriceNote);

public sealed record SupplierDetailsDto(
    int Id,
    string CompanyName,
    int GovernorateId,
    string GovernorateName,
    string? Description,
    double Latitude,
    double Longitude,
    bool IsPublic,
    bool IsOwner,
    string OwnerUserId,
    IReadOnlyList<SupplierMaterialDto> Materials);

public sealed record SaveSupplierDto(
    string CompanyName,
    int GovernorateId,
    string? Description,
    double? Latitude,
    double? Longitude,
    bool IsPublic);

public sealed record SaveSupplierMaterialDto(
    int RawMaterialId,
    decimal? MinOrderTons,
    string? PriceNote);

