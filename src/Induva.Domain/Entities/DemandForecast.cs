using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class DemandForecast : BaseEntity
{
    public string RequestedByUserId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string? Category { get; set; }
    public int? GovernorateId { get; set; }
    public Governorate? Governorate { get; set; }
    public int HorizonMonths { get; set; } = 12;
    public string Trend { get; set; } = "Stable";
    public double Confidence { get; set; }
    public string ResultJson { get; set; } = "{}";
    public int? AiRunId { get; set; }
}

