using Induva.Domain.Common;

namespace Induva.Domain.Entities;

public class Governorate : BaseEntity
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

