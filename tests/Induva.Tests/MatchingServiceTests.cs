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

public class MatchingServiceTests
{
    private static (MatchingService svc, ApplicationDbContext db, IAiJsonRunner ai) CreateService(string userId)
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

        var svc = new MatchingService(new UnitOfWork(db), user, ai, opts);
        return (svc, db, ai);
    }

    [Fact]
    public async Task Unsupported_pair_returns_validation_error()
    {
        var (svc, _, _) = CreateService("user-1");

        // Supplier to Investor is not a supported matching flow
        var r = await svc.FindMatchesAsync(MatchEntityType.Supplier, 1, MatchEntityType.Investor);

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Validation, r.Kind);
    }

    [Fact]
    public async Task Matching_with_ai_failure_falls_back_to_rules_only()
    {
        var (svc, db, ai) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة", Latitude = 30.04, Longitude = 31.23 });
        db.RawMaterials.Add(new RawMaterial { Id = 10, NameEn = "Polypropylene", NameAr = "بولي بروبيلين", Category = "Polymers" });

        var factory = new Factory
        {
            Id = 1,
            OwnerUserId = "user-owner",
            Name = "Plastic Products Co",
            GovernorateId = 1,
            Latitude = 30.04,
            Longitude = 31.23
        };
        factory.MaterialNeeds.Add(new FactoryMaterialNeed { FactoryId = 1, RawMaterialId = 10 });
        db.Factories.Add(factory);

        var sup = new Supplier
        {
            Id = 20,
            CompanyName = "Nile Polymers",
            GovernorateId = 1,
            Latitude = 30.05,
            Longitude = 31.24,
            IsPublic = true,
            OwnerUserId = "sup-owner"
        };
        sup.Materials.Add(new SupplierMaterial { SupplierId = 20, RawMaterialId = 10 });
        db.Suppliers.Add(sup);
        await db.SaveChangesAsync();

        // AI call fails with an error
        ai.RunAsync<MatchRerankResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<MatchRerankResult>>.Fail(ErrorKind.AiUnavailable, "Provider unreachable"));

        var r = await svc.FindMatchesAsync(MatchEntityType.Factory, 1, MatchEntityType.Supplier);

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);
        Assert.False(r.Value.AiUsed); // Fallback: AiUsed = false
        Assert.Single(r.Value.Matches);

        var match = r.Value.Matches[0];
        Assert.Equal(20, match.Id);
        Assert.True(match.FinalScore > 90); // Scored by rules
        Assert.Contains(match.Reasons, rk => rk.Contains("required raw materials"));
    }

    [Fact]
    public async Task Unknown_candidate_ids_from_ai_are_discarded()
    {
        var (svc, db, ai) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.RawMaterials.Add(new RawMaterial { Id = 5, NameEn = "Silica", NameAr = "سيليكا", Category = "Minerals" });

        var factory = new Factory
        {
            Id = 1, OwnerUserId = "user-owner", Name = "Glass Factory", GovernorateId = 1
        };
        factory.MaterialNeeds.Add(new FactoryMaterialNeed { FactoryId = 1, RawMaterialId = 5 });
        db.Factories.Add(factory);

        var legitSupplier = new Supplier
        {
            Id = 100, CompanyName = "Real Silica", GovernorateId = 1, IsPublic = true, OwnerUserId = "owner-100"
        };
        legitSupplier.Materials.Add(new SupplierMaterial { SupplierId = 100, RawMaterialId = 5 });
        db.Suppliers.Add(legitSupplier);
        await db.SaveChangesAsync();

        // AI returns a hallucinated candidate ID 999 not in DB candidates!
        var mockAi = new MatchRerankResult
        {
            Matches =
            [
                new() { CandidateId = 999, Adjustment = 10, Reasons = ["Hallucinated"], NextStep = "N/A" },
                new() { CandidateId = 100, Adjustment = 5, Reasons = ["Excellent purity"], NextStep = "Call them" }
            ]
        };

        ai.RunAsync<MatchRerankResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<MatchRerankResult>>.Ok(new AiOutput<MatchRerankResult>(mockAi, 301, false)));

        var r = await svc.FindMatchesAsync(MatchEntityType.Factory, 1, MatchEntityType.Supplier);

        Assert.True(r.Succeeded);
        Assert.Single(r.Value!.Matches);
        Assert.Equal(100, r.Value.Matches[0].Id); // Only the legit candidate retained
        Assert.DoesNotContain(r.Value.Matches, m => m.Id == 999);
    }
}

