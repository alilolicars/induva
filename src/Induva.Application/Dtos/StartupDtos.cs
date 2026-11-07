using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record StartupFilter(
    int? GovernorateId = null,
    Sector? Sector = null,
    StartupStage? Stage = null,
    string? Q = null,
    int Page = 1,
    int PageSize = 12);

public sealed record StartupListItemDto(
    int Id,
    string Name,
    Sector Sector,
    StartupStage Stage,
    string GovernorateName,
    decimal? FundingNeededEgp);

public sealed record StartupDetailsDto(
    int Id,
    string Name,
    Sector Sector,
    StartupStage Stage,
    string? Description,
    string ProductIdea,
    decimal? FundingNeededEgp,
    IReadOnlyList<string> RequiredTags,
    int? GovernorateId,
    string GovernorateName,
    bool IsOwner,
    string OwnerUserId);

public sealed record SaveStartupDto(
    string Name,
    Sector Sector,
    StartupStage Stage,
    string? Description,
    string ProductIdea,
    decimal? FundingNeededEgp,
    IReadOnlyList<string> RequiredTags,
    int? GovernorateId);

