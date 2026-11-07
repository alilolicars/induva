using Induva.Application.Abstractions;
using Induva.Application.Dtos;
using Induva.Domain.Entities;
using Induva.Domain.Enums;

namespace Induva.Application.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardStatsAsync(IReadOnlyList<AdminUserDto> users, CancellationToken ct = default);
}

public sealed class AdminDashboardService(IUnitOfWork uow) : IAdminDashboardService
{
    public async Task<AdminDashboardDto> GetDashboardStatsAsync(IReadOnlyList<AdminUserDto> users, CancellationToken ct = default)
    {
        // 1. Entity counts
        var totalFactories = await uow.Repository<Factory>().CountAsync(null, ct);
        var totalSuppliers = await uow.Repository<Supplier>().CountAsync(null, ct);
        var totalStartups = await uow.Repository<Startup>().CountAsync(null, ct);
        var totalInvestors = await uow.Repository<InvestorProfile>().CountAsync(null, ct);
        var totalOpenDemands = await uow.Repository<DemandRequest>().CountAsync(d => d.Status == "Open", ct);

        // User role breakdown
        var roleCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users)
        {
            foreach (var r in u.Roles)
            {
                roleCounts[r] = roleCounts.GetValueOrDefault(r, 0) + 1;
            }
        }

        var counts = new AdminCountsDto(
            users.Count,
            roleCounts,
            totalFactories,
            totalSuppliers,
            totalStartups,
            totalInvestors,
            totalOpenDemands);

        // 2. AI Usage stats & recent runs
        var allRuns = await uow.Repository<AiAnalysisRun>().ListAsync(null, ct);
        var totalAiRuns = allRuns.Count;
        var successfulAiRuns = allRuns.Count(r => r.Status == AiRunStatus.Succeeded);
        var failedAiRuns = allRuns.Count(r => r.Status == AiRunStatus.Failed);
        var avgLatency = totalAiRuns > 0 ? allRuns.Average(r => r.LatencyMs) : 0.0;

        var runsByFeature = allRuns
            .GroupBy(r => r.Feature.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Last 7 days distribution
        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
        var last7Days = new List<DailyAiRunDto>();
        for (int i = 0; i < 7; i++)
        {
            var dt = sevenDaysAgo.AddDays(i);
            var dateStr = dt.ToString("yyyy-MM-dd");
            var dayCount = allRuns.Count(r => r.CreatedAtUtc.Date == dt);
            last7Days.Add(new DailyAiRunDto(dateStr, dayCount));
        }

        var aiStats = new AiUsageStatsDto(
            totalAiRuns,
            successfulAiRuns,
            failedAiRuns,
            Math.Round(avgLatency, 1),
            runsByFeature,
            last7Days);

        // Last 50 runs (NO raw prompts or raw completions)
        var recentRuns = allRuns
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(50)
            .Select(r => new RecentAiRunDto(
                r.Id,
                r.Feature,
                r.EntityType,
                r.EntityId,
                r.LatencyMs,
                r.Status == AiRunStatus.Succeeded,
                r.ErrorMessage,
                r.LatencyMs == 0 || r.Model.Equals("demo", StringComparison.OrdinalIgnoreCase),
                r.CreatedAtUtc))
            .ToList();

        return new AdminDashboardDto(counts, aiStats, recentRuns, users);
    }
}
