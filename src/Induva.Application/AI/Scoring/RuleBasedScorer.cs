using Induva.Application.Common;
using Induva.Domain.Enums;

namespace Induva.Application.AI.Scoring;

public sealed record GeoPoint(double Lat, double Lng);

public static class RuleBasedScorer
{
    public static double FactoryToSupplier(
        IReadOnlySet<int> factoryNeedIds,
        GeoPoint factory,
        IReadOnlySet<int> supplierMaterialIds,
        GeoPoint supplier)
    {
        var coverage = factoryNeedIds.Count == 0 ? 0.0 : (double)factoryNeedIds.Intersect(supplierMaterialIds).Count() / factoryNeedIds.Count;
        var proximity = Geo.ProximityScore(Geo.HaversineKm(factory.Lat, factory.Lng, supplier.Lat, supplier.Lng));
        return Math.Round(100 * (0.7 * coverage + 0.3 * proximity), 1);
    }

    public static double FactoryToDemand(
        IReadOnlySet<string> factoryTags,
        GeoPoint factory,
        IReadOnlySet<string> requiredTags,
        GeoPoint buyer,
        decimal? monthlyCapacityHint,
        decimal requestedQuantity,
        DateTime? deadlineUtc)
    {
        var tagScore = requiredTags.Count == 0 ? 0.0 : (double)requiredTags.Count(factoryTags.Contains) / requiredTags.Count;
        var proximity = Geo.ProximityScore(Geo.HaversineKm(factory.Lat, factory.Lng, buyer.Lat, buyer.Lng));
        var volumeFit = monthlyCapacityHint is > 0 ? Math.Min(1.0, (double)(monthlyCapacityHint.Value / Math.Max(1, requestedQuantity))) : 0.5;
        var deadline = deadlineUtc is null ? 0.7 : (deadlineUtc.Value - DateTime.UtcNow).TotalDays switch
        {
            < 7 => 0.2,
            < 30 => 0.6,
            _ => 1.0
        };

        return Math.Round(100 * (0.5 * tagScore + 0.2 * proximity + 0.2 * volumeFit + 0.1 * deadline), 1);
    }

    public static double FactoryToInvestor(
        Sector factorySector,
        int utilizationPercent,
        bool hasCurrentIdleWindow,
        IReadOnlySet<Sector> investorSectors)
    {
        var sectorMatch = investorSectors.Count == 0 || investorSectors.Contains(factorySector) ? 1.0 : 0.0;
        var idleBonus = hasCurrentIdleWindow ? 1.0 : Math.Clamp((100 - utilizationPercent) / 100.0, 0.0, 1.0);
        return Math.Round(100 * (0.7 * sectorMatch + 0.3 * idleBonus), 1);
    }

    public static double StartupToFactory(
        IReadOnlySet<string> startupRequiredTags,
        GeoPoint startup,
        IReadOnlySet<string> factoryTags,
        GeoPoint factory,
        int factoryUtilization,
        bool hasIdleWindow)
    {
        var tagOverlap = startupRequiredTags.Count == 0 ? 0.0 : (double)startupRequiredTags.Count(factoryTags.Contains) / startupRequiredTags.Count;
        var proximity = Geo.ProximityScore(Geo.HaversineKm(startup.Lat, startup.Lng, factory.Lat, factory.Lng));
        var capacityBonus = hasIdleWindow || factoryUtilization < 60 ? 1.0 : (factoryUtilization < 85 ? 0.5 : 0.1);
        return Math.Round(100 * (0.6 * tagOverlap + 0.2 * proximity + 0.2 * capacityBonus), 1);
    }

    public static double StartupToInvestor(
        Sector startupSector,
        StartupStage startupStage,
        decimal? fundingNeededEgp,
        IReadOnlySet<Sector> investorSectors,
        IReadOnlySet<StartupStage> investorStages,
        decimal? minTicketEgp,
        decimal? maxTicketEgp)
    {
        var sectorMatch = investorSectors.Count == 0 || investorSectors.Contains(startupSector) ? 1.0 : 0.0;
        var stageMatch = investorStages.Count == 0 || investorStages.Contains(startupStage) ? 1.0 : 0.0;

        double ticketFit;
        if (!fundingNeededEgp.HasValue)
        {
            ticketFit = 0.5;
        }
        else
        {
            var val = fundingNeededEgp.Value;
            var fitsMin = !minTicketEgp.HasValue || val >= minTicketEgp.Value;
            var fitsMax = !maxTicketEgp.HasValue || val <= maxTicketEgp.Value;
            ticketFit = fitsMin && fitsMax ? 1.0 : 0.2;
        }

        return Math.Round(100 * (0.4 * sectorMatch + 0.3 * ticketFit + 0.3 * stageMatch), 1);
    }
}

