using Induva.Application.Dtos;
using Induva.Domain.Entities;

namespace Induva.Application.Mappings;

public static class StartupMappings
{
    public static StartupListItemDto ToListItem(this Startup s) =>
        new(s.Id, s.Name, s.Sector, s.Stage, s.Governorate?.NameEn ?? "National", s.FundingNeededEgp);

    public static StartupDetailsDto ToDetails(this Startup s, bool isOwner)
    {
        var tags = string.IsNullOrWhiteSpace(s.RequiredTagsCsv)
            ? Array.Empty<string>()
            : s.RequiredTagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new StartupDetailsDto(
            s.Id,
            s.Name,
            s.Sector,
            s.Stage,
            s.Description,
            s.ProductIdea,
            s.FundingNeededEgp,
            tags,
            s.GovernorateId,
            s.Governorate?.NameEn ?? "National",
            isOwner,
            s.OwnerUserId);
    }
}

