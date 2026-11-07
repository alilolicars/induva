using Induva.Application.Dtos;
using Induva.Domain.Entities;

namespace Induva.Application.Mappings;

public static class FactoryMappings
{
    public static FactoryListItemDto ToListItem(this Factory f) =>
        new(f.Id, f.Name, f.Sector, f.Governorate?.NameEn ?? "", f.UtilizationPercent, f.Machines?.Count ?? 0);

    public static MachineDto ToDto(this Machine m) =>
        new(m.Id, m.Name, m.MachineType, m.Quantity, m.CapacityPerHour, m.CapacityUnit, m.Description);

    public static IdleWindowDto ToDto(this IdleWindow w) =>
        new(w.Id, w.StartMonth, w.EndMonth, w.AvailableCapacityPercent, w.Note);

    public static FactoryProductDto ToDto(this FactoryProduct p) =>
        new(p.Id, p.Name, p.Category);

    public static FactoryMaterialNeedDto ToDto(this FactoryMaterialNeed n) =>
        new(n.Id, n.RawMaterialId, n.RawMaterial?.NameEn ?? "", n.MonthlyTons);

    public static GovernorateDto ToDto(this Governorate g) =>
        new(g.Id, g.NameEn, g.NameAr, g.Latitude, g.Longitude);

    public static RawMaterialDto ToDto(this RawMaterial m) =>
        new(m.Id, m.NameEn, m.NameAr, m.Category);
}

