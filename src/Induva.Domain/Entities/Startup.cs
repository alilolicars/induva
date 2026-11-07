using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class Startup : BaseEntity
{
    public string OwnerUserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Sector Sector { get; set; }
    public StartupStage Stage { get; set; }
    public string? Description { get; set; }
    public string ProductIdea { get; set; } = default!;
    public decimal? FundingNeededEgp { get; set; }
    public string RequiredTagsCsv { get; set; } = "";
    public int? GovernorateId { get; set; }
    public Governorate? Governorate { get; set; }
}

