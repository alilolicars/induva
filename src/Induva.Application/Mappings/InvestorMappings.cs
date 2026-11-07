using Induva.Application.Dtos;
using Induva.Domain.Entities;

namespace Induva.Application.Mappings;

public static class InvestorMappings
{
    public static InvestorListItemDto ToListItem(this InvestorProfile i)
    {
        var sectors = string.IsNullOrWhiteSpace(i.SectorsCsv)
            ? Array.Empty<string>()
            : i.SectorsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var stages = string.IsNullOrWhiteSpace(i.PreferredStagesCsv)
            ? Array.Empty<string>()
            : i.PreferredStagesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new InvestorListItemDto(i.Id, i.DisplayName, sectors, i.MinTicketEgp, i.MaxTicketEgp, stages);
    }

    public static InvestorDetailsDto ToDetails(this InvestorProfile i, bool isOwner)
    {
        var sectors = string.IsNullOrWhiteSpace(i.SectorsCsv)
            ? Array.Empty<string>()
            : i.SectorsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var stages = string.IsNullOrWhiteSpace(i.PreferredStagesCsv)
            ? Array.Empty<string>()
            : i.PreferredStagesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new InvestorDetailsDto(
            i.Id,
            i.DisplayName,
            sectors,
            i.MinTicketEgp,
            i.MaxTicketEgp,
            stages,
            i.Bio,
            isOwner,
            i.OwnerUserId);
    }
}

