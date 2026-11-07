using Induva.Application.Dtos;
using Induva.Domain.Entities;

namespace Induva.Application.Mappings;

public static class SupplierMappings
{
    public static SupplierListItemDto ToListItem(this Supplier s) =>
        new(s.Id, s.CompanyName, s.Governorate?.NameEn ?? "", s.Materials?.Count ?? 0);

    public static SupplierMaterialDto ToDto(this SupplierMaterial m) =>
        new(m.Id, m.RawMaterialId, m.RawMaterial?.NameEn ?? "", m.RawMaterial?.Category ?? "", m.MinOrderTons, m.PriceNote);
}

