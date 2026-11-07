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

public class FactoryAnalysisServiceTests
{
    private static (FactoryAnalysisService svc, ApplicationDbContext db, IAiJsonRunner ai) CreateService(string userId)
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

        var svc = new FactoryAnalysisService(new UnitOfWork(db), user, ai, opts);
        return (svc, db, ai);
    }

    [Fact]
    public async Task Analysis_fails_if_factory_has_no_machines()
    {
        var (svc, db, _) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory
        {
            Id = 1, OwnerUserId = "user-owner", Name = "Empty Factory",
            GovernorateId = 1, Description = "Some description"
        });
        await db.SaveChangesAsync();

        var r = await svc.AnalyzeAsync(1);

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Validation, r.Kind);
        Assert.Contains("machine", r.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Analysis_fails_for_non_owner()
    {
        var (svc, db, _) = CreateService("user-intruder");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory
        {
            Id = 1, OwnerUserId = "user-legit", Name = "Legit Factory",
            GovernorateId = 1, Description = "desc"
        });
        await db.SaveChangesAsync();

        var r = await svc.AnalyzeAsync(1);

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);
    }

    [Fact]
    public async Task Successful_analysis_normalizes_tags_and_persists_capability_profile()
    {
        var (svc, db, ai) = CreateService("user-owner");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Sharqia", NameAr = "الشرقية" });
        var factory = new Factory
        {
            Id = 1, OwnerUserId = "user-owner", Name = "Delta Plastics",
            GovernorateId = 1, Description = "Blow molding plant"
        };
        factory.Machines.Add(new Machine { FactoryId = 1, Name = "Extrusion Blow Molding Machine", Quantity = 2 });
        db.Factories.Add(factory);
        await db.SaveChangesAsync();

        var mockAiResult = new FactoryAnalysisResult
        {
            Summary = "Factory specializes in blow molding and extrusion.",
            CapabilityTags = new List<string>
            {
                "BLOW-MOLDING",      // Case normalization needed
                "extrusion",         // Valid tag
                "fake-unknown-tag",  // Should be filtered out
                "blow-molding"       // Duplicate should be deduplicated
            },
            Strengths = new List<string> { "High precision" },
            Confidence = 0.9
        };

        ai.RunAsync<FactoryAnalysisResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<FactoryAnalysisResult>>.Ok(new AiOutput<FactoryAnalysisResult>(mockAiResult, RunId: 99, FromCache: false)));

        var r = await svc.AnalyzeAsync(1);

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);

        // Tags must be normalized: lowercase, valid, distinct
        Assert.Contains("blow-molding", r.Value.CapabilityTags);
        Assert.Contains("extrusion", r.Value.CapabilityTags);
        Assert.DoesNotContain("fake-unknown-tag", r.Value.CapabilityTags);
        Assert.Equal(2, r.Value.CapabilityTags.Count);

        // Capability profile must be persisted in database
        var profile = await db.FactoryCapabilityProfiles.FirstOrDefaultAsync(p => p.FactoryId == 1);
        Assert.NotNull(profile);
        Assert.Equal("blow-molding,extrusion", profile.TagsCsv);
        Assert.Equal(99, profile.AiRunId);
    }
}

