using Induva.Application.Services;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Induva.Tests;

public class MapServiceTests
{
    private static (MapService svc, ApplicationDbContext db) CreateContext()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        return (new MapService(new UnitOfWork(db)), db);
    }

    [Fact]
    public async Task GetMarkers_returns_markers_for_requested_layers()
    {
        var (svc, db) = CreateContext();

        db.Governorates.AddRange(
            new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة", Latitude = 30.04, Longitude = 31.23 },
            new Governorate { Id = 2, NameEn = "Alexandria", NameAr = "الإسكندرية", Latitude = 31.20, Longitude = 29.91 });

        db.RawMaterials.Add(new RawMaterial { Id = 1, NameEn = "Limestone", NameAr = "حجر جيري", Category = "Minerals" });

        db.Factories.Add(new Factory
        {
            Id = 1,
            Name = "Cairo Plastic Works",
            Sector = Sector.Plastics,
            GovernorateId = 1,
            Latitude = 30.05,
            Longitude = 31.24,
            IsPublic = true,
            OwnerUserId = "owner-1"
        });

        db.Suppliers.Add(new Supplier
        {
            Id = 2,
            CompanyName = "Alex Feedstock Corp",
            GovernorateId = 2,
            Latitude = 31.21,
            Longitude = 29.92,
            IsPublic = true,
            OwnerUserId = "sup-1"
        });

        db.GovernorateResources.Add(new GovernorateResource
        {
            Id = 3,
            GovernorateId = 1,
            RawMaterialId = 1,
            EstimatedAnnualTons = 500000
        });

        db.DemandRequests.Add(new DemandRequest
        {
            Id = 4,
            Title = "Need 10,000 PET Bottles",
            ProductName = "PET Bottles",
            Sector = Sector.Plastics,
            Quantity = 10000,
            Unit = "bottles",
            GovernorateId = 1,
            Status = "Open",
            RequesterUserId = "buyer-1"
        });

        await db.SaveChangesAsync();

        // 1. Fetch all layers
        var allMarkers = await svc.GetMarkersAsync(["factories", "suppliers", "resources", "demand"]);
        Assert.Equal(4, allMarkers.Count);
        Assert.Contains(allMarkers, m => m.Type == "factories" && m.Title == "Cairo Plastic Works");
        Assert.Contains(allMarkers, m => m.Type == "suppliers" && m.Title == "Alex Feedstock Corp");
        Assert.Contains(allMarkers, m => m.Type == "resources");
        Assert.Contains(allMarkers, m => m.Type == "demand" && m.Title == "Need 10,000 PET Bottles");

        // 2. Filter by governorateId = 2 (Alexandria)
        var alexMarkers = await svc.GetMarkersAsync(["factories", "suppliers"], governorateId: 2);
        Assert.Single(alexMarkers);
        Assert.Equal("suppliers", alexMarkers[0].Type);
        Assert.Equal("Alex Feedstock Corp", alexMarkers[0].Title);
    }
}

