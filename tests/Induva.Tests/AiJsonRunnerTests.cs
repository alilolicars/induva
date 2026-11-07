using Induva.Application.AI;
using Induva.Application.Common;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class AiJsonRunnerTests
{
    private record TestResult(string Title, int Value);

    private static (AiJsonRunner runner, ApplicationDbContext db, IAiClient client) CreateRunner(int dailyLimit = 30)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var client = Substitute.For<IAiClient>();
        var opts = Options.Create(new AiOptions { DailyLimitPerUser = dailyLimit, CacheHours = 24 });
        var log = NullLogger<AiJsonRunner>.Instance;

        var runner = new AiJsonRunner(client, new UnitOfWork(db), opts, log);
        return (runner, db, client);
    }

    [Fact]
    public async Task Valid_response_succeeds_and_persists_run()
    {
        var (runner, db, client) = CreateRunner();
        client.ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiResponse("{\"title\":\"Good\",\"value\":42}", "{\"title\":\"Good\",\"value\":42}"));

        var call = new AiJsonCall(AiFeature.FactoryAnalysis, "user-1", "Factory", 1, "sys", "usr", "v1");
        var res = await runner.RunAsync<TestResult>(call);

        Assert.True(res.Succeeded);
        Assert.NotNull(res.Value);
        Assert.Equal("Good", res.Value.Data.Title);
        Assert.Equal(42, res.Value.Data.Value);
        Assert.False(res.Value.FromCache);

        // Verify audit persisted
        var runs = await db.AiAnalysisRuns.ToListAsync();
        Assert.Single(runs);
        Assert.Equal(AiRunStatus.Succeeded, runs[0].Status);
        Assert.Equal("user-1", runs[0].UserId);
    }

    [Fact]
    public async Task Invalid_response_triggers_repair_retry_and_succeeds()
    {
        var (runner, db, client) = CreateRunner();
        client.ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => new AiResponse("invalid garbage before", "invalid garbage before"),
                _ => new AiResponse("{\"title\":\"Repaired\",\"value\":100}", "{\"title\":\"Repaired\",\"value\":100}")
            );

        var call = new AiJsonCall(AiFeature.FactoryAnalysis, "user-1", "Factory", 1, "sys", "usr", "v1");
        var res = await runner.RunAsync<TestResult>(call);

        Assert.True(res.Succeeded);
        Assert.Equal("Repaired", res.Value!.Data.Title);

        // Verify two calls were made to client
        await client.Received(2).ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Persistent_invalid_response_fails_and_audits_failure()
    {
        var (runner, db, client) = CreateRunner();
        client.ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiResponse("not json at all", "not json at all"));

        var call = new AiJsonCall(AiFeature.FactoryAnalysis, "user-1", "Factory", 1, "sys", "usr", "v1");
        var res = await runner.RunAsync<TestResult>(call);

        Assert.False(res.Succeeded);
        Assert.Equal(ErrorKind.AiUnavailable, res.Kind);

        var runs = await db.AiAnalysisRuns.ToListAsync();
        Assert.Single(runs);
        Assert.Equal(AiRunStatus.Failed, runs[0].Status);
    }

    [Fact]
    public async Task Provider_429_maps_to_rate_limited()
    {
        var (runner, db, client) = CreateRunner();
        client.ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>())
            .Returns<AiResponse>(_ => throw new AiProviderException(429, "Too many requests"));

        var call = new AiJsonCall(AiFeature.FactoryAnalysis, "user-1", "Factory", 1, "sys", "usr", "v1");
        var res = await runner.RunAsync<TestResult>(call);

        Assert.False(res.Succeeded);
        Assert.Equal(ErrorKind.RateLimited, res.Kind);
    }

    [Fact]
    public async Task Daily_limit_blocks_new_calls_when_exhausted()
    {
        var (runner, db, client) = CreateRunner(dailyLimit: 2);

        // Insert 2 runs for today
        db.AiAnalysisRuns.Add(new AiAnalysisRun
        {
            UserId = "user-quota", Feature = AiFeature.FactoryAnalysis, EntityType = "Factory", EntityId = 1,
            PromptVersion = "v1", Model = "m", InputHash = "h1", CreatedAtUtc = DateTime.UtcNow
        });
        db.AiAnalysisRuns.Add(new AiAnalysisRun
        {
            UserId = "user-quota", Feature = AiFeature.FactoryAnalysis, EntityType = "Factory", EntityId = 1,
            PromptVersion = "v1", Model = "m", InputHash = "h2", CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var call = new AiJsonCall(AiFeature.FactoryAnalysis, "user-quota", "Factory", 1, "sys", "usr", "v1");
        var res = await runner.RunAsync<TestResult>(call);

        Assert.False(res.Succeeded);
        Assert.Equal(ErrorKind.RateLimited, res.Kind);
        await client.DidNotReceiveWithAnyArgs().ChatAsync(default!);
    }

    [Fact]
    public async Task Cache_hit_returns_cached_data_and_makes_no_ai_calls()
    {
        var (runner, db, client) = CreateRunner();
        client.ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiResponse("{\"title\":\"FromLive\",\"value\":10}", "raw"));

        var call = new AiJsonCall(AiFeature.FactoryAnalysis, "user-1", "Factory", 1, "sys", "usr", "v1");

        // First call: live
        var res1 = await runner.RunAsync<TestResult>(call);
        Assert.True(res1.Succeeded);
        Assert.False(res1.Value!.FromCache);

        // Second call: should hit cache
        var res2 = await runner.RunAsync<TestResult>(call);
        Assert.True(res2.Succeeded);
        Assert.True(res2.Value!.FromCache);
        Assert.Equal("FromLive", res2.Value.Data.Title);

        // Client was called only once!
        await client.Received(1).ChatAsync(Arg.Any<AiRequest>(), Arg.Any<CancellationToken>());
    }
}

