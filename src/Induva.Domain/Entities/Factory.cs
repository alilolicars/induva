using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class Factory : BaseEntity
{
    public string OwnerUserId { get; set; } = default!;      // Identity user id (no FK — keeps Domain pure)
    public string Name { get; set; } = default!;
    public Sector Sector { get; set; }
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public int UtilizationPercent { get; set; }              // 0..100
    public bool IsPublic { get; set; } = true;

    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    public ICollection<IdleWindow> IdleWindows { get; set; } = new List<IdleWindow>();
    public ICollection<FactoryProduct> Products { get; set; } = new List<FactoryProduct>();
    public ICollection<FactoryMaterialNeed> MaterialNeeds { get; set; } = new List<FactoryMaterialNeed>();
    public FactoryCapabilityProfile? CapabilityProfile { get; set; }
    public ICollection<ProductSuggestion> ProductSuggestions { get; set; } = new List<ProductSuggestion>();
}

public class Machine : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? MachineType { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? CapacityPerHour { get; set; }
    public string? CapacityUnit { get; set; }                // e.g. "kg", "pieces", "liters"
    public string? Description { get; set; }
}

public class IdleWindow : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public int StartMonth { get; set; }                      // 1..12
    public int EndMonth { get; set; }                        // 1..12 (may wrap year-end, e.g. 11 → 2)
    public int AvailableCapacityPercent { get; set; }       // 0..100
    public string? Note { get; set; }
}

public class FactoryProduct : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
}

public class FactoryMaterialNeed : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = default!;
    public decimal? MonthlyTons { get; set; }
}

public class FactoryCapabilityProfile : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public string TagsCsv { get; set; } = "";
    public string ResultJson { get; set; } = "{}";
    public int? AiRunId { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

