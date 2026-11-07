using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class AiAnalysisRun : BaseEntity
{
    public AiFeature Feature { get; set; }
    public string UserId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public int EntityId { get; set; }
    public string PromptVersion { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string InputHash { get; set; } = default!;
    public string? RawResponse { get; set; }
    public string? ParsedJson { get; set; }
    public AiRunStatus Status { get; set; } = AiRunStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int LatencyMs { get; set; }
}

