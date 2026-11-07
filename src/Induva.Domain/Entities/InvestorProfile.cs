using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class InvestorProfile : BaseEntity
{
    public string OwnerUserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string SectorsCsv { get; set; } = "";
    public decimal? MinTicketEgp { get; set; }
    public decimal? MaxTicketEgp { get; set; }
    public string PreferredStagesCsv { get; set; } = "";
    public string? Bio { get; set; }
}

