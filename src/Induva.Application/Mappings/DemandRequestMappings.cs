using Induva.Application.Dtos;
using Induva.Domain.Entities;

namespace Induva.Application.Mappings;

public static class DemandRequestMappings
{
    public static DemandRequestListItemDto ToListItem(this DemandRequest d) =>
        new(d.Id, d.Title, d.ProductName, d.Sector, d.Governorate?.NameEn ?? "", d.Quantity, d.Unit, d.TargetPriceEgp, d.IsExport, d.Status, d.DeadlineUtc);

    public static DemandRequestDetailsDto ToDetails(this DemandRequest d, bool isOwner)
    {
        var tags = string.IsNullOrWhiteSpace(d.RequiredTagsCsv)
            ? Array.Empty<string>()
            : d.RequiredTagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new DemandRequestDetailsDto(
            d.Id,
            d.Title,
            d.ProductName,
            d.Sector,
            tags,
            d.Quantity,
            d.Unit,
            d.TargetPriceEgp,
            d.DeadlineUtc,
            d.GovernorateId,
            d.Governorate?.NameEn ?? "",
            d.IsExport,
            d.Status,
            d.Description,
            isOwner,
            d.RequesterUserId);
    }
}

