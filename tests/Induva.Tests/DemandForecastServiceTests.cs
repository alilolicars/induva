using Induva.Application.Abstractions;
using Induva.Application.AI;
using Induva.Application.AI.Features;
using Induva.Application.AI.Results;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class DemandForecastServiceTests
{
    private static (DemandForecastService svc, ApplicationDbContext db, IAiJsonRunner ai) CreateService(string userId)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(true);

        var ai = Substitute.For<IAiJsonRunner>();
        var opts = Options.Create(new AiOptions { OutputLanguage = "en" });

        var svc = new DemandForecastService(new UnitOfWork(db), user, ai, opts);
        return (svc, db, ai);
    }

    [Fact]
    public async Task Server_assigns_month_labels_starting_from_next_calendar_month()
    {
        var (svc, db, ai) = CreateService("user-1");

        var mockAi = new DemandForecastResult
        {
            Trend = "Rising",
            MonthlyIndex = [40, 50, 60, 70, 80, 90],
            PeakMonths = [6],
            Drivers = ["Summer beverage surge"],
            DataBasis = "Mixed",
            Confidence = 0.85
        };

        ai.RunAsync<DemandForecastResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<DemandForecastResult>>.Ok(new AiOutput<DemandForecastResult>(mockAi, 201, false)));

        var r = await svc.ForecastAsync(new GenerateDemandForecastDto("Glass Bottles", "Packaging", null, HorizonMonths: 6));

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);
        Assert.Equal(6, r.Value.MonthLabels.Count);

        // Next month label
        var expectedFirstMonth = DateTime.UtcNow.AddMonths(1).ToString("MMM yyyy");
        Assert.Equal(expectedFirstMonth, r.Value.MonthLabels[0]);
    }

    [Fact]
    public async Task Post_processing_pads_or_trims_index_and_lowers_confidence_when_length_differs()
    {
        var (svc, db, ai) = CreateService("user-1");

        // Request horizon is 6, but model returned only 3 values!
        var mockAiShort = new DemandForecastResult
        {
            Trend = "Falling",
            MonthlyIndex = [80.0, 70.0, 60.0], // Length 3 instead of 6
            Confidence = 0.95 // Model claims 0.95 confidence
        };

        ai.RunAsync<DemandForecastResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<DemandForecastResult>>.Ok(new AiOutput<DemandForecastResult>(mockAiShort, 202, false)));

        var r = await svc.ForecastAsync(new GenerateDemandForecastDto("Cardboard Boxes", HorizonMonths: 6));

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);

        // Must be padded to exactly 6 values
        Assert.Equal(6, r.Value.MonthlyIndex.Count);
        // Confidence must be capped to <= 0.3
        Assert.True(r.Value.Confidence <= 0.3);
        // Assumption must note adjustment
        Assert.Contains("Forecast length adjusted", r.Value.Assumptions);
    }

    [Fact]
    public async Task Post_processing_clamps_values_and_falls_back_for_unknown_trend()
    {
        var (svc, db, ai) = CreateService("user-1");

        var mockAi = new DemandForecastResult
        {
            Trend = "WildlyBooming", // Unknown trend string
            MonthlyIndex = [-15.0, 150.0, 50.0, 60.0, 70.0, 80.0], // Out-of-range values: -15 and 150
            Confidence = 1.5 // Out-of-range confidence
        };

        ai.RunAsync<DemandForecastResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<DemandForecastResult>>.Ok(new AiOutput<DemandForecastResult>(mockAi, 203, false)));

        var r = await svc.ForecastAsync(new GenerateDemandForecastDto("Wheat Flour", HorizonMonths: 6));

        Assert.True(r.Succeeded);
        Assert.NotNull(r.Value);

        // Clamped values
        Assert.Equal(0.0, r.Value.MonthlyIndex[0]);
        Assert.Equal(100.0, r.Value.MonthlyIndex[1]);

        // Trend fallback to Stable
        Assert.Equal("Stable", r.Value.Trend);

        // Confidence clamped to 1.0 (or <= 0.3 if length was adjusted)
        Assert.True(r.Value.Confidence <= 1.0);
    }

    [Fact]
    public async Task Forecast_persists_in_database_and_can_be_retrieved()
    {
        var (svc, db, ai) = CreateService("user-1");

        var mockAi = new DemandForecastResult
        {
            Trend = "Rising",
            MonthlyIndex = [10, 20, 30, 40, 50, 60],
            PeakMonths = [6],
            Drivers = ["Export surge"],
            DataBasis = "PlatformData",
            Confidence = 0.8
        };

        ai.RunAsync<DemandForecastResult>(Arg.Any<AiJsonCall>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiOutput<DemandForecastResult>>.Ok(new AiOutput<DemandForecastResult>(mockAi, 204, false)));

        var rCreate = await svc.ForecastAsync(new GenerateDemandForecastDto("Plastic Crates", HorizonMonths: 6));
        Assert.True(rCreate.Succeeded);

        // Retrieve latest forecast from DB
        var rGet = await svc.GetLatestForecastAsync("Plastic Crates");
        Assert.True(rGet.Succeeded);
        Assert.NotNull(rGet.Value);
        Assert.Equal("Plastic Crates", rGet.Value.ProductName);
        Assert.Equal("Rising", rGet.Value.Trend);
        Assert.Equal(6, rGet.Value.MonthlyIndex.Count);
        Assert.Equal("PlatformData", rGet.Value.DataBasis);
    }
}

