using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class InvestmentAnalysis : BaseEntity
{
    public string RequestedByUserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int? FactoryId { get; set; }
    public Factory? Factory { get; set; }
    public int? StartupId { get; set; }
    public Startup? Startup { get; set; }
    public int? ProductSuggestionId { get; set; }
    public ProductSuggestion? ProductSuggestion { get; set; }
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = default!;
    public string AssumptionsJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
    public int? PaybackMonths { get; set; }
    public decimal? ThreeYearRoi { get; set; }
    public int? AiRunId { get; set; }
}

