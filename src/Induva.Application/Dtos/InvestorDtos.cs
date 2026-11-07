using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record InvestorFilter(
    Sector? Sector = null,
    StartupStage? Stage = null,
    string? Q = null,
    int Page = 1,
    int PageSize = 12);

public sealed record InvestorListItemDto(
    int Id,
    string DisplayName,
    IReadOnlyList<string> Sectors,
    decimal? MinTicketEgp,
    decimal? MaxTicketEgp,
    IReadOnlyList<string> PreferredStages);

public sealed record InvestorDetailsDto(
    int Id,
    string DisplayName,
    IReadOnlyList<string> Sectors,
    decimal? MinTicketEgp,
    decimal? MaxTicketEgp,
    IReadOnlyList<string> PreferredStages,
    string? Bio,
    bool IsOwner,
    string OwnerUserId);

public sealed record SaveInvestorProfileDto(
    string DisplayName,
    IReadOnlyList<Sector> Sectors,
    decimal? MinTicketEgp,
    decimal? MaxTicketEgp,
    IReadOnlyList<StartupStage> PreferredStages,
    string? Bio);

