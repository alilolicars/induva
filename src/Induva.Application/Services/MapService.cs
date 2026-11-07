using Induva.Application.Abstractions;
using Induva.Application.Dtos;
using Induva.Domain.Entities;

namespace Induva.Application.Services;

public interface IMapService
{
    Task<IReadOnlyList<MapMarkerDto>> GetMarkersAsync(string[] layers, int? governorateId = null, CancellationToken ct = default);
}

public sealed class MapService(IUnitOfWork uow) : IMapService
{
    public async Task<IReadOnlyList<MapMarkerDto>> GetMarkersAsync(string[] layers, int? governorateId = null, CancellationToken ct = default)
    {
        var markers = new List<MapMarkerDto>();
        var layerSet = new HashSet<string>(layers, StringComparer.OrdinalIgnoreCase);

        // 1. Factories layer
        if (layerSet.Contains("factories"))
        {
            var factories = await uow.Repository<Factory>().ListAsync(
                f => f.IsPublic && (governorateId == null || f.GovernorateId == governorateId),
                ct,
                f => f.Governorate);

            foreach (var f in factories)
            {
                markers.Add(new MapMarkerDto(
                    "factories",
                    f.Id,
                    f.Name,
                    $"{f.Sector} · {f.Governorate?.NameEn ?? ""}",
                    f.Latitude,
                    f.Longitude,
                    $"/Factories/Details/{f.Id}"));
            }
        }

        // 2. Suppliers layer
        if (layerSet.Contains("suppliers"))
        {
            var suppliers = await uow.Repository<Supplier>().ListAsync(
                s => s.IsPublic && (governorateId == null || s.GovernorateId == governorateId),
                ct,
                s => s.Governorate);

            foreach (var s in suppliers)
            {
                markers.Add(new MapMarkerDto(
                    "suppliers",
                    s.Id,
                    s.CompanyName,
                    $"Supplier · {s.Governorate?.NameEn ?? ""}",
                    s.Latitude,
                    s.Longitude,
                    $"/Suppliers/Details/{s.Id}"));
            }
        }

        // 3. Raw Material Resources layer
        if (layerSet.Contains("resources"))
        {
            var govResources = await uow.Repository<GovernorateResource>().ListAsync(
                r => governorateId == null || r.GovernorateId == governorateId,
                ct,
                r => r.Governorate,
                r => r.RawMaterial);

            var grouped = govResources.GroupBy(r => r.GovernorateId);
            foreach (var group in grouped)
            {
                var gov = group.First().Governorate;
                if (gov is null) continue;

                var materialNames = group.Select(r => r.RawMaterial?.NameEn ?? "Unknown").Distinct();
                markers.Add(new MapMarkerDto(
                    "resources",
                    gov.Id,
                    $"{gov.NameEn} Raw Materials",
                    string.Join(", ", materialNames),
                    gov.Latitude,
                    gov.Longitude,
                    $"/Map?governorateId={gov.Id}"));
            }
        }

        // 4. Demand layer
        if (layerSet.Contains("demand"))
        {
            var demands = await uow.Repository<DemandRequest>().ListAsync(
                d => d.Status == "Open" && (governorateId == null || d.GovernorateId == governorateId),
                ct,
                d => d.Governorate);

            foreach (var d in demands)
            {
                markers.Add(new MapMarkerDto(
                    "demand",
                    d.Id,
                    d.Title,
                    $"{d.ProductName} ({d.Quantity:N0} {d.Unit}) · {d.Governorate?.NameEn ?? ""}",
                    d.Governorate?.Latitude ?? 30.0,
                    d.Governorate?.Longitude ?? 31.0,
                    $"/DemandRequests/Details/{d.Id}"));
            }
        }

        return markers;
    }
}

