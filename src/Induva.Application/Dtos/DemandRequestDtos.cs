using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record DemandRequestFilter(
    int? GovernorateId = null,
    Sector? Sector = null,
    bool? IsExport = null,
    string? Status = "Open",
    string? Q = null,
    int Page = 1,
    int PageSize = 12);

public sealed record DemandRequestListItemDto(
    int Id,
    string Title,
    string ProductName,
    Sector Sector,
    string GovernorateName,
    decimal Quantity,
    string Unit,
    decimal? TargetPriceEgp,
    bool IsExport,
    string Status,
    DateTime? DeadlineUtc);

public sealed record DemandRequestDetailsDto(
    int Id,
    string Title,
    string ProductName,
    Sector Sector,
    IReadOnlyList<string> RequiredTags,
    decimal Quantity,
    string Unit,
    decimal? TargetPriceEgp,
    DateTime? DeadlineUtc,
    int GovernorateId,
    string GovernorateName,
    bool IsExport,
    string Status,
    string? Description,
    bool IsOwner,
    string RequesterUserId);

public sealed record SaveDemandRequestDto(
    string Title,
    string ProductName,
    Sector Sector,
    IReadOnlyList<string> RequiredTags,
    decimal Quantity,
    string Unit,
    decimal? TargetPriceEgp,
    DateTime? DeadlineUtc,
    int GovernorateId,
    bool IsExport,
    string Status,
    string? Description);

