namespace Induva.Application.Dtos;

public sealed record GovernorateResourceDto(
    int Id,
    int GovernorateId,
    string GovernorateName,
    int RawMaterialId,
    string RawMaterialName,
    string Category,
    decimal? EstimatedAnnualTons,
    string? Notes);

public sealed record SaveGovernorateResourceDto(
    int GovernorateId,
    int RawMaterialId,
    decimal? EstimatedAnnualTons,
    string? Notes);

