using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record MatchCandidateDto(
    int Id,
    string Title,
    string Subtitle,
    string? Location,
    double DistanceKm,
    double RuleScore,
    double FinalScore,
    IReadOnlyList<string> Reasons,
    string? NextStep,
    string TargetOwnerUserId,
    bool AiUsed);

public sealed record MatchingQueryDto(
    MatchEntityType SourceType,
    int SourceId,
    MatchEntityType TargetType);

public sealed record MatchingSummaryDto(
    MatchEntityType SourceType,
    int SourceId,
    string SourceTitle,
    MatchEntityType TargetType,
    IReadOnlyList<MatchCandidateDto> Matches,
    bool AiUsed,
    DateTime GeneratedAtUtc);

