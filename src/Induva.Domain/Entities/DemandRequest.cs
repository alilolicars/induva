using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class DemandRequest : BaseEntity
{
    public string RequesterUserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public Sector Sector { get; set; }
    public string RequiredTagsCsv { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!; // e.g. "tons", "units", "meters"
    public decimal? TargetPriceEgp { get; set; }
    public DateTime? DeadlineUtc { get; set; }
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = default!;
    public bool IsExport { get; set; }
    public string Status { get; set; } = "Open"; // Open, Closed
    public string? Description { get; set; }
}

