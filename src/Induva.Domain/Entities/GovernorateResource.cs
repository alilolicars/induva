using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class GovernorateResource : BaseEntity
{
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = default!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = default!;

    public decimal? EstimatedAnnualTons { get; set; }
    public string? Notes { get; set; }
}

