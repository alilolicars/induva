using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class Supplier : BaseEntity
{
    public string OwnerUserId { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;

    public ICollection<SupplierMaterial> Materials { get; set; } = new List<SupplierMaterial>();
}

public class SupplierMaterial : BaseEntity
{
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = default!;

    public decimal? MinOrderTons { get; set; }
    public string? PriceNote { get; set; }
}

