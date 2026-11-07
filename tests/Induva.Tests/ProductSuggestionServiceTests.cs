using Induva.Application.Abstractions;
using Induva.Application.AI;
using Induva.Application.AI.Features;
using Induva.Application.AI.Results;
using Induva.Application.Common;
using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class ProductSuggestionServiceTests
{
    private static (ProductSuggestionService svc, ApplicationDbContext db, IAiJsonRunner ai) CreateService(string userId)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(true);
        user.IsInRole(Roles.Admin).Returns(false);

        var ai = Substitute.For<IAiJsonRunner>();
        var opts = Options.Create(new AiOptions { OutputLanguage = "en" });

        var svc = new ProductSuggestionService(new UnitOfWork(db), user, ai, opts);
        return (svc, db, ai);
    }

    [Fact]
    public async Task Suggest_fails_if_factory_has_no_capability_profile()
    {
        var (svc, db, _) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Alexandria", NameAr = "الإسكندرية" });
        db.Factories.Add(new Factory
        {
            Id = 1, OwnerUserId = "user-owner", Name = "Unanalyzed Factory",
            GovernorateId = 1, Description = "desc"
        });
        await db.SaveChangesAsync();

        var r = await svc.SuggestAsync(1);

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Validation, r.Kind);
        Assert.Contains("analysis", r.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Suggest_drops_invalid_items_and_keeps_maximum_five()
    {
        var (svc, db, ai) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        var factory = new Factory
        {
            Id = 1, OwnerUserId = "user-owner", Name = "Plastic Molders",
            GovernorateId = 1
        };
        factory.Machines.Add(new Machine { FactoryId = 1, Name = "Injection 300T", Quantity = 1 });
        factory.CapabilityProfile = new FactoryCapabilityProfile
        {
            FactoryId = 1, Summary = "Injection molding facility", TagsCsv = "injection-molding"
        };
        db.Factories.Add(factory);
        await db.SaveChangesAsync();

        var mockAi = new ProductSuggestionResult
        {
            Suggestions = new List<SuggestedProduct>
            {
                new() { Name = "", EstUnitPriceEgp = 50, EstUnitCostEgp = 30 }, // Invalid: empty name
                new() { Name = "Bad Price", EstUnitPriceEgp = 0, EstUnitCostEgp = 30 }, // Invalid: zero price
                new() { Name = "Negative Cost", EstUnitPriceEgp = 50, EstUnitCostEgp = -5 }, // Invalid: negative cost
                new() { Name = "Valid 1", EstUnitPriceEgp = 50, EstUnitCostEgp = 30, RequiredMachines = ["Injection 300T"] },
                new() { Name = "Valid 2", EstUnitPriceEgp = 60, EstUnitCostEgp = 35, RequiredMachines = ["Injection 300T"] },
                new() { Name = "Valid 3", EstUnitPriceEgp = 70, EstUnitCostEgp = 40, RequiredMachines = ["Injection 300T"] },
                new() { Name = "Valid 4", EstUnitPriceEgp = 80, EstUnitCostEgp = 45, RequiredMachines = ["Injection 300T"] },
                new() { Name = "Valid 5", EstUnitPriceEgp = 90, EstUnitCostEgp = 50, RequiredMachines = ["Injection 300T"] },
                new() { Name = "Valid 6 (Excess)", EstUnitPriceEgp = 100, EstUnitCostEgp = 55, RequiredMachines = ["Injection 300T"] } // Should be dropped (max 5)
            }
        };

        ai.RunAsync<ProductSuggestionResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<ProductSuggestionResult>>.Ok(new AiOutput<ProductSuggestionResult>(mockAi, 101, false)));

        var r = await svc.SuggestAsync(1);

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);
        Assert.Equal(5, r.Value.Suggestions.Count);
        Assert.DoesNotContain(r.Value.Suggestions, s => s.Name == "Valid 6 (Excess)");
        Assert.DoesNotContain(r.Value.Suggestions, s => s.Name == "Bad Price");
    }

    [Fact]
    public async Task Suggest_flags_unlisted_machines_and_calculates_margin_in_csharp()
    {
        var (svc, db, ai) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        var factory = new Factory
        {
            Id = 2, OwnerUserId = "user-owner", Name = "Precision Metals",
            GovernorateId = 1
        };
        factory.Machines.Add(new Machine { FactoryId = 2, Name = "CNC Lathe 5-Axis", Quantity = 1 });
        factory.CapabilityProfile = new FactoryCapabilityProfile
        {
            FactoryId = 2, Summary = "CNC machining shop", TagsCsv = "cnc-machining"
        };
        db.Factories.Add(factory);
        await db.SaveChangesAsync();

        var mockAi = new ProductSuggestionResult
        {
            Suggestions = new List<SuggestedProduct>
            {
                // Item with unlisted machine (Hydraulic Press) and Thin Margin ((100 - 97)/100 = 3%)
                new()
                {
                    Name = "Heavy Metal Flange",
                    EstUnitPriceEgp = 100,
                    EstUnitCostEgp = 97, // 3% margin -> should get "Thin margin"
                    RequiredMachines = ["CNC Lathe 5-Axis", "Hydraulic Press 500T"], // Hydraulic Press not in factory!
                    Confidence = 0.8
                },
                // Item with unusually high margin ((100 - 20)/100 = 80%)
                new()
                {
                    Name = "Titanium Dental Implant",
                    EstUnitPriceEgp = 100,
                    EstUnitCostEgp = 20, // 80% margin -> should get "Margin looks unusually high — verify costs"
                    RequiredMachines = ["CNC Lathe 5-Axis"],
                    Confidence = 0.95
                }
            }
        };

        ai.RunAsync<ProductSuggestionResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<ProductSuggestionResult>>.Ok(new AiOutput<ProductSuggestionResult>(mockAi, 102, false)));

        var r = await svc.SuggestAsync(2);

        Assert.True(r.Succeeded);
        var flange = r.Value!.Suggestions.First(s => s.Name == "Heavy Metal Flange");
        var implant = r.Value.Suggestions.First(s => s.Name == "Titanium Dental Implant");

        // Margin computed in C#
        Assert.Equal(3.0m, flange.MarginPercent);
        Assert.Equal(80.0m, implant.MarginPercent);

        // Risks added deterministically in C#
        Assert.Contains(flange.Risks, rk => rk.Contains("not listed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(flange.Risks, rk => rk == "Thin margin");
        Assert.Contains(implant.Risks, rk => rk.Contains("unusually high", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Suggest_replaces_suggested_items_while_preserving_saved_items()
    {
        var (svc, db, ai) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        var factory = new Factory
        {
            Id = 3, OwnerUserId = "user-owner", Name = "Pack Co",
            GovernorateId = 1
        };
        factory.Machines.Add(new Machine { FactoryId = 3, Name = "Blow Molder", Quantity = 1 });
        factory.CapabilityProfile = new FactoryCapabilityProfile
        {
            FactoryId = 3, Summary = "Blow molding", TagsCsv = "blow-molding"
        };
        db.Factories.Add(factory);

        // Pre-existing saved item
        db.ProductSuggestions.Add(new ProductSuggestion
        {
            Id = 10,
            FactoryId = 3,
            Name = "Previously Saved Bottle",
            Rationale = "Important client",
            EstUnitPriceEgp = 50,
            EstUnitCostEgp = 30,
            Status = SuggestionStatus.Saved
        });

        // Pre-existing suggested item (should be replaced)
        db.ProductSuggestions.Add(new ProductSuggestion
        {
            Id = 11,
            FactoryId = 3,
            Name = "Old Transient Suggestion",
            Rationale = "Old",
            EstUnitPriceEgp = 40,
            EstUnitCostEgp = 25,
            Status = SuggestionStatus.Suggested
        });

        await db.SaveChangesAsync();

        var mockAi = new ProductSuggestionResult
        {
            Suggestions = new List<SuggestedProduct>
            {
                new()
                {
                    Name = "New Fresh Suggestion",
                    EstUnitPriceEgp = 60,
                    EstUnitCostEgp = 35,
                    RequiredMachines = ["Blow Molder"],
                    Confidence = 0.85
                }
            }
        };

        ai.RunAsync<ProductSuggestionResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<ProductSuggestionResult>>.Ok(new AiOutput<ProductSuggestionResult>(mockAi, 103, false)));

        var r = await svc.SuggestAsync(3);

        Assert.True(r.Succeeded);

        // Check database state directly
        var inDb = await db.ProductSuggestions.Where(s => s.FactoryId == 3).ToListAsync();

        // Old transient suggested item was deleted
        Assert.DoesNotContain(inDb, s => s.Id == 11);

        // Previously saved item was preserved
        var savedItem = inDb.FirstOrDefault(s => s.Id == 10);
        Assert.NotNull(savedItem);
        Assert.Equal(SuggestionStatus.Saved, savedItem.Status);

        // New suggestion was added
        Assert.Contains(inDb, s => s.Name == "New Fresh Suggestion" && s.Status == SuggestionStatus.Suggested);
    }
}

