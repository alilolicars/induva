using Induva.Domain.Enums;

namespace Induva.Application.AI.Results;

public class ProductSuggestionResult
{
    public List<SuggestedProduct> Suggestions { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
}

public class SuggestedProduct
{
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string Rationale { get; set; } = "";
    public List<string> RequiredMachines { get; set; } = [];
    public List<string> RequiredRawMaterials { get; set; } = [];
    public List<string> RequiredCapabilityTags { get; set; } = [];
    public decimal EstUnitCostEgp { get; set; }
    public decimal EstUnitPriceEgp { get; set; }
    public decimal EstMonthlyVolume { get; set; }
    public DemandLevel DemandLevel { get; set; } = DemandLevel.Medium;
    public string? SeasonalityNote { get; set; }
    public double Confidence { get; set; }
    public List<string> Risks { get; set; } = [];
}

