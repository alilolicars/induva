using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class ProductSuggestion : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public int? AiRunId { get; set; }
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
    public string Rationale { get; set; } = default!;
    public string RequiredMachinesCsv { get; set; } = "";
    public string RequiredMaterialsCsv { get; set; } = "";
    public decimal EstUnitCostEgp { get; set; }
    public decimal EstUnitPriceEgp { get; set; }
    public decimal EstMonthlyVolume { get; set; }
    public DemandLevel DemandLevel { get; set; } = DemandLevel.Medium;
    public string? SeasonalityNote { get; set; }
    public double Confidence { get; set; }
    public string RisksJson { get; set; } = "[]";
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Suggested;
}

