using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record ProductSuggestionItemDto(
    int Id,
    string Name,
    string? Category,
    string Rationale,
    IReadOnlyList<string> RequiredMachines,
    IReadOnlyList<string> RequiredRawMaterials,
    IReadOnlyList<string> RequiredCapabilityTags,
    decimal EstUnitCostEgp,
    decimal EstUnitPriceEgp,
    decimal EstMonthlyVolume,
    decimal MarginPercent,
    DemandLevel DemandLevel,
    string? SeasonalityNote,
    double Confidence,
    IReadOnlyList<string> Risks,
    SuggestionStatus Status);

public sealed record ProductSuggestionSummaryDto(
    int FactoryId,
    string FactoryName,
    IReadOnlyList<ProductSuggestionItemDto> Suggestions,
    IReadOnlyList<string> Assumptions,
    bool FromCache,
    DateTime GeneratedAtUtc);