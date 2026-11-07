using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Induva.Tests;

public class AdminDashboardServiceTests
{
    private static (AdminDashboardService svc, ApplicationDbContext db) CreateContext()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        return (new AdminDashboardService(new UnitOfWork(db)), db);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_aggregates_entity_counts_and_role_breakdown()
    {
        var (svc, db) = CreateContext();

        // Seed entities
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.AddRange(
            new Factory { Id = 1, Name = "F1", GovernorateId = 1, OwnerUserId = "u1" },
            new Factory { Id = 2, Name = "F2", GovernorateId = 1, OwnerUserId = "u2" }
        );
        db.Suppliers.Add(new Supplier { Id = 1, CompanyName = "S1", GovernorateId = 1, OwnerUserId = "u3" });
        db.Startups.Add(new Startup { Id = 1, Name = "ST1", Sector = Sector.Electronics, ProductIdea = "Smart IoT Device", OwnerUserId = "u4" });
        db.InvestorProfiles.Add(new InvestorProfile { Id = 1, DisplayName = "Inv1", OwnerUserId = "u5" });
        db.DemandRequests.AddRange(
            new DemandRequest { Id = 1, Title = "Open RFQ 1", ProductName = "P1", Unit = "tons", GovernorateId = 1, Status = "Open", RequesterUserId = "u1" },
            new DemandRequest { Id = 2, Title = "Open RFQ 2", ProductName = "P2", Unit = "tons", GovernorateId = 1, Status = "Open", RequesterUserId = "u1" },
            new DemandRequest { Id = 3, Title = "Closed RFQ", ProductName = "P3", Unit = "tons", GovernorateId = 1, Status = "Fulfilled", RequesterUserId = "u1" }
        );

        await db.SaveChangesAsync();

        var users = new List<AdminUserDto>
        {
            new("u1", "admin@induva.com", "Admin", new[] { "Admin" }, false, DateTime.UtcNow),
            new("u2", "factory1@induva.com", "Factory 1", new[] { "Factory" }, false, DateTime.UtcNow),
            new("u3", "factory2@induva.com", "Factory 2", new[] { "Factory" }, false, DateTime.UtcNow),
            new("u4", "supplier@induva.com", "Supplier", new[] { "Supplier" }, false, DateTime.UtcNow),
            new("u5", "investor@induva.com", "Investor", new[] { "Investor" }, true, DateTime.UtcNow)
        };

        var result = await svc.GetDashboardStatsAsync(users);

        Assert.Equal(5, result.Counts.TotalUsers);
        Assert.Equal(2, result.Counts.TotalFactories);
        Assert.Equal(1, result.Counts.TotalSuppliers);
        Assert.Equal(1, result.Counts.TotalStartups);
        Assert.Equal(1, result.Counts.TotalInvestors);
        Assert.Equal(2, result.Counts.TotalOpenDemands);

        Assert.Equal(1, result.Counts.UsersByRole["Admin"]);
        Assert.Equal(2, result.Counts.UsersByRole["Factory"]);
        Assert.Equal(1, result.Counts.UsersByRole["Supplier"]);
        Assert.Equal(1, result.Counts.UsersByRole["Investor"]);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_computes_ai_telemetry_correctly()
    {
        var (svc, db) = CreateContext();

        var today = DateTime.UtcNow;

        db.AiAnalysisRuns.AddRange(
            new AiAnalysisRun
            {
                Id = 1,
                Feature = AiFeature.FactoryAnalysis,
                UserId = "u1",
                EntityType = "Factory",
                EntityId = 10,
                PromptVersion = "v1",
                Model = "deepseek.v3.2",
                InputHash = "hash1",
                Status = AiRunStatus.Succeeded,
                LatencyMs = 1200,
                CreatedAtUtc = today
            },
            new AiAnalysisRun
            {
                Id = 2,
                Feature = AiFeature.ProductSuggestion,
                UserId = "u1",
                EntityType = "Factory",
                EntityId = 10,
                PromptVersion = "v1",
                Model = "deepseek.v3.2",
                InputHash = "hash2",
                Status = AiRunStatus.Succeeded,
                LatencyMs = 800,
                CreatedAtUtc = today
            },
            new AiAnalysisRun
            {
                Id = 3,
                Feature = AiFeature.Matching,
                UserId = "u2",
                EntityType = "Supplier",
                EntityId = 5,
                PromptVersion = "v1",
                Model = "deepseek.v3.2",
                InputHash = "hash3",
                Status = AiRunStatus.Failed,
                ErrorMessage = "Network timeout",
                LatencyMs = 5000,
                CreatedAtUtc = today.AddDays(-2)
            }
        );

        await db.SaveChangesAsync();

        var result = await svc.GetDashboardStatsAsync(new List<AdminUserDto>());

        Assert.Equal(3, result.AiStats.TotalRuns);
        Assert.Equal(2, result.AiStats.SuccessfulRuns);
        Assert.Equal(1, result.AiStats.FailedRuns);
        Assert.Equal(2333.3, result.AiStats.AverageLatencyMs);

        Assert.Equal(1, result.AiStats.RunsByFeature[AiFeature.FactoryAnalysis.ToString()]);
        Assert.Equal(1, result.AiStats.RunsByFeature[AiFeature.ProductSuggestion.ToString()]);
        Assert.Equal(1, result.AiStats.RunsByFeature[AiFeature.Matching.ToString()]);

        Assert.Equal(7, result.AiStats.Last7Days.Count);
        Assert.Equal(3, result.RecentRuns.Count);

        var firstRecent = result.RecentRuns[0];
        Assert.Equal(today.Date, firstRecent.CreatedAtUtc.Date);
        Assert.True(firstRecent.Success);
    }
}
