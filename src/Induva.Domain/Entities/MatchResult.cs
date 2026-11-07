using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class MatchResult : BaseEntity
{
    public string RequestedByUserId { get; set; } = default!;
    public MatchEntityType SourceType { get; set; }
    public int SourceId { get; set; }
    public MatchEntityType TargetType { get; set; }
    public int TargetId { get; set; }
    public double RuleScore { get; set; }
    public double AiAdjustment { get; set; }
    public double FinalScore { get; set; }
    public string ReasonsJson { get; set; } = "[]";
    public string? NextStep { get; set; }
    public int? AiRunId { get; set; }
}

