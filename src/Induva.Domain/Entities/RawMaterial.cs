using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class RawMaterial : BaseEntity
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Category { get; set; } = default!; // Mineral / Agricultural / Chemical / Other
}

