using Induva.Application.Abstractions;
using Induva.Application.AI;
using Induva.Application.AI.Features;
using Induva.Application.AI.Results;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Entities;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class InvestmentServiceTests
{
    private static (InvestmentService svc, ApplicationDbContext db, IAiJsonRunner ai) CreateService(string userId)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(true);

        var ai = Substitute.For<IAiJsonRunner>();
        var opts = Options.Create(new AiOptions { OutputLanguage = "en" });

        var svc = new InvestmentService(new UnitOfWork(db), user, ai, opts);
        return (svc, db, ai);
    }

    [Fact]
    public async Task Calculate_does_not_call_ai_and_computes_metrics()
    {
        var (svc, db, ai) = CreateService("user-1");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Alexandria", NameAr = "الإسكندرية" });
        await db.SaveChangesAsync();

        var req = new CalculateInvestmentRequestDto(
            ProductName: "Packaging Crates",
            Category: "Packaging",
            GovernorateId: 1,
            FactoryId: null,
            CapexEgp: 600000,
            MonthlyVolumeUnits: 5000,
            UnitPriceEgp: 50,
            UnitMaterialCostEgp: 20,
            UnitLaborCostEgp: 5,
            UnitEnergyCostEgp: 3,
            UnitTransportCostEgp: 2, // Variable cost: 30, Margin: 20 (40%)
            MonthlyFixedCostsEgp: 20000,
            RampUpMonths: 2,
            Years: 3,
            AnnualDiscountRate: 0.20m);

        var r = await svc.CalculateAsync(req);

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);

        // Core principle: AI runner was NOT invoked
        await ai.DidNotReceiveWithAnyArgs().RunAsync<object>(default!, default);

        // Verify scenarios generated
        Assert.Equal(3, r.Value.Scenarios.Count);
        var baseScen = r.Value.Scenarios.First(s => s.Name == "Base").Metrics;
        Assert.Equal(20m, baseScen.UnitMarginEgp);
        Assert.Equal(40.0m, baseScen.MarginPercent);

        // Saved in database
        var saved = await db.InvestmentAnalyses.FindAsync(r.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal("Packaging Crates", r.Value.ProductName);
    }

    [Fact]
    public async Task Calculate_flags_margin_and_capex_warnings()
    {
        var (svc, db, _) = CreateService("user-1");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        await db.SaveChangesAsync();

        // 1. Test losing money (negative margin)
        var reqLosing = new CalculateInvestmentRequestDto(
            ProductName: "Unprofitable Product",
            Category: null,
            GovernorateId: 1,
            FactoryId: null,
            CapexEgp: 100000,
            MonthlyVolumeUnits: 1000,
            UnitPriceEgp: 10,
            UnitMaterialCostEgp: 15, // Cost > Price!
            UnitLaborCostEgp: 5,
            UnitEnergyCostEgp: 2,
            UnitTransportCostEgp: 1,
            MonthlyFixedCostsEgp: 5000,
            RampUpMonths: 0);

        var rLosing = await svc.CalculateAsync(reqLosing);
        Assert.True(rLosing.Succeeded);
        Assert.Contains(rLosing.Value!.Warnings, w => w.Contains("loses money", StringComparison.OrdinalIgnoreCase));

        // 2. Test Capex = 0
        var reqZeroCapex = new CalculateInvestmentRequestDto(
            ProductName: "Zero Capex Line",
            Category: null,
            GovernorateId: 1,
            FactoryId: null,
            CapexEgp: 0, // Zero Capex
            MonthlyVolumeUnits: 2000,
            UnitPriceEgp: 100,
            UnitMaterialCostEgp: 30,
            UnitLaborCostEgp: 5,
            UnitEnergyCostEgp: 3,
            UnitTransportCostEgp: 2,
            MonthlyFixedCostsEgp: 10000,
            RampUpMonths: 1);

        var rZeroCapex = await svc.CalculateAsync(reqZeroCapex);
        Assert.True(rZeroCapex.Succeeded);
        Assert.Contains(rZeroCapex.Value!.Warnings, w => w.Contains("Capex is zero", StringComparison.OrdinalIgnoreCase));
    }
}

