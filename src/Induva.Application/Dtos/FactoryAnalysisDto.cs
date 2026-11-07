using Induva.Application.AI.Results;

namespace Induva.Application.Dtos;

public sealed record FactoryAnalysisDto(
    string Summary,
    IReadOnlyList<string> CapabilityTags,
    IReadOnlyList<ProcessCapability> Processes,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> UnderusedAssets,
    IReadOnlyList<string> MissingInformation,
    double Confidence,
    DateTime GeneratedAtUtc,
    bool FromCache,
    string? Warning = null);

