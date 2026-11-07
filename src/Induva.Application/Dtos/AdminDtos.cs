using Induva.Domain.Enums;

namespace Induva.Application.Dtos;

public sealed record AdminCountsDto(
    int TotalUsers,
    IReadOnlyDictionary<string, int> UsersByRole,
    int TotalFactories,
    int TotalSuppliers,
    int TotalStartups,
    int TotalInvestors,
    int TotalOpenDemands);

public sealed record AiUsageStatsDto(
    int TotalRuns,
    int SuccessfulRuns,
    int FailedRuns,
    double AverageLatencyMs,
    IReadOnlyDictionary<string, int> RunsByFeature,
    IReadOnlyList<DailyAiRunDto> Last7Days);

public sealed record DailyAiRunDto(
    string Date,
    int RunCount);

public sealed record RecentAiRunDto(
    int Id,
    AiFeature Feature,
    string EntityType,
    int EntityId,
    int LatencyMs,
    bool Success,
    string? Error,
    bool FromCache,
    DateTime CreatedAtUtc);

public sealed record AdminUserDto(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    bool IsLockedOut,
    DateTime CreatedAtUtc);

public sealed record AdminDashboardDto(
    AdminCountsDto Counts,
    AiUsageStatsDto AiStats,
    IReadOnlyList<RecentAiRunDto> RecentRuns,
    IReadOnlyList<AdminUserDto> Users);
