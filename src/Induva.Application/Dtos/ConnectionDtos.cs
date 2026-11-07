using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record ConnectionRequestDto(
    int Id,
    string FromUserId,
    string FromDisplayName,
    string ToUserId,
    string ToDisplayName,
    MatchEntityType RelatedType,
    int RelatedId,
    string RelatedTitle,
    string Message,
    ConnectionStatus Status,
    DateTime CreatedAtUtc,
    DateTime? RespondedAtUtc);

public sealed record SendConnectionRequestDto(
    string ToUserId,
    MatchEntityType RelatedType,
    int RelatedId,
    string Message);

