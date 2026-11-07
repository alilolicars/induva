using System.Text.Json;
using Induva.Application.Abstractions;
using Induva.Application.AI.Prompts;
using Induva.Application.AI.Results;
using Induva.Application.AI.Scoring;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Induva.Application.AI.Features;

public interface IMatchingService
{
    Task<Result<MatchingSummaryDto>> FindMatchesAsync(MatchEntityType source, int sourceId, MatchEntityType target, CancellationToken ct = default);
}

public sealed class MatchingService(
    IUnitOfWork uow,
    ICurrentUserService user,
    IAiJsonRunner ai,
    IOptions<AiOptions> opts) : IMatchingService
{
    private static readonly HashSet<(MatchEntityType, MatchEntityType)> SupportedPairs =
    [
        (MatchEntityType.Factory, MatchEntityType.Supplier),
        (MatchEntityType.Factory, MatchEntityType.DemandRequest),
        (MatchEntityType.Factory, MatchEntityType.Investor),
        (MatchEntityType.Startup, MatchEntityType.Factory),
        (MatchEntityType.Startup, MatchEntityType.Investor)
    ];

    public async Task<Result<MatchingSummaryDto>> FindMatchesAsync(MatchEntityType source, int sourceId, MatchEntityType target, CancellationToken ct = default)
    {
        if (!SupportedPairs.Contains((source, target)))
            return Result<MatchingSummaryDto>.Fail(ErrorKind.Validation, $"Matching from {source} to {target} is not supported.");

        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<MatchingSummaryDto>.Fail(ErrorKind.Forbidden, "Please log in to find matches.");

        string sourceTitle = $"{source} #{sourceId}";
        List<PreScoredCandidate> scoredCandidates = [];

        // 1. Process specific source/target pairs
        if (source == MatchEntityType.Factory)
        {
            var f = await uow.Repository<Factory>().GetByIdAsync(
                sourceId,
                ct,
                x => x.Governorate,
                x => x.MaterialNeeds,
                x => x.Machines,
                x => x.IdleWindows,
                x => x.CapabilityProfile!);

            if (f is null) return Result<MatchingSummaryDto>.Fail(ErrorKind.NotFound, "Factory not found.");
            if (f.OwnerUserId != user.UserId && !user.IsInRole(Roles.Admin))
                return Result<MatchingSummaryDto>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

            sourceTitle = f.Name;
            var fGeo = new GeoPoint(f.Latitude, f.Longitude);

            if (target == MatchEntityType.Supplier)
            {
                var factoryNeedIds = f.MaterialNeeds.Select(n => n.RawMaterialId).ToHashSet();
                var suppliers = await uow.Repository<Supplier>().ListAsync(
                    s => s.IsPublic,
                    ct,
                    s => s.Governorate,
                    s => s.Materials);

                foreach (var sup in suppliers.Take(50))
                {
                    var supGeo = new GeoPoint(sup.Latitude, sup.Longitude);
                    var supMaterialIds = sup.Materials.Select(m => m.RawMaterialId).ToHashSet();
                    var score = RuleBasedScorer.FactoryToSupplier(factoryNeedIds, fGeo, supMaterialIds, supGeo);
                    var dist = Geo.HaversineKm(f.Latitude, f.Longitude, sup.Latitude, sup.Longitude);

                    var matchedCount = factoryNeedIds.Intersect(supMaterialIds).Count();
                    var defaultReason = factoryNeedIds.Count > 0
                        ? $"Covers {matchedCount} of {factoryNeedIds.Count} required raw materials · {dist:N0} km away"
                        : $"Regional raw material supplier · {dist:N0} km away";

                    scoredCandidates.Add(new PreScoredCandidate(
                        sup.Id,
                        sup.CompanyName,
                        $"{sup.Materials.Count} supplied materials",
                        sup.Governorate?.NameEn ?? "Egypt",
                        dist,
                        score,
                        defaultReason,
                        sup.OwnerUserId));
                }
            }
            else if (target == MatchEntityType.DemandRequest)
            {
                var fTags = (f.CapabilityProfile?.TagsCsv ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var demands = await uow.Repository<DemandRequest>().ListAsync(
                    d => d.Status == "Open",
                    ct,
                    d => d.Governorate);

                foreach (var d in demands.Take(50))
                {
                    var dGov = d.Governorate;
                    var dGeo = new GeoPoint(dGov?.Latitude ?? 30.0, dGov?.Longitude ?? 31.0);
                    var dTags = (d.RequiredTagsCsv ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var score = RuleBasedScorer.FactoryToDemand(fTags, fGeo, dTags, dGeo, null, d.Quantity, d.DeadlineUtc);
                    var dist = Geo.HaversineKm(f.Latitude, f.Longitude, dGeo.Lat, dGeo.Lng);
                    var overlap = dTags.Count(fTags.Contains);

                    scoredCandidates.Add(new PreScoredCandidate(
                        d.Id,
                        d.Title,
                        $"{d.ProductName} ({d.Quantity:N0} {d.Unit})",
                        dGov?.NameEn ?? "Egypt",
                        dist,
                        score,
                        $"{overlap} overlapping capability tags · {dist:N0} km away",
                        d.RequesterUserId));
                }
            }
            else if (target == MatchEntityType.Investor)
            {
                var investors = await uow.Repository<InvestorProfile>().ListAsync(null, ct);
                var currentMonth = DateTime.UtcNow.Month;
                var hasIdle = f.IdleWindows.Any(w => w.StartMonth <= currentMonth && currentMonth <= w.EndMonth);

                foreach (var inv in investors.Take(50))
                {
                    var invSectors = (inv.SectorsCsv ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => Enum.TryParse<Sector>(s, out var sec) ? sec : (Sector?)null)
                        .Where(s => s.HasValue).Select(s => s!.Value).ToHashSet();

                    var score = RuleBasedScorer.FactoryToInvestor(f.Sector, f.UtilizationPercent, hasIdle, invSectors);
                    var sectorMatch = invSectors.Count == 0 || invSectors.Contains(f.Sector);

                    scoredCandidates.Add(new PreScoredCandidate(
                        inv.Id,
                        inv.DisplayName,
                        inv.Bio ?? "Industrial Investor",
                        "Egypt",
                        0,
                        score,
                        sectorMatch ? $"Sector alignment with {f.Sector} · Available capacity" : "Industrial fund partner",
                        inv.OwnerUserId));
                }
            }
        }
        else if (source == MatchEntityType.Startup)
        {
            var s = await uow.Repository<Startup>().GetByIdAsync(sourceId, ct, x => x.Governorate!);
            if (s is null) return Result<MatchingSummaryDto>.Fail(ErrorKind.NotFound, "Startup not found.");
            if (s.OwnerUserId != user.UserId && !user.IsInRole(Roles.Admin))
                return Result<MatchingSummaryDto>.Fail(ErrorKind.Forbidden, "You do not own this startup.");

            sourceTitle = s.Name;
            var sGeo = new GeoPoint(s.Governorate?.Latitude ?? 30.0, s.Governorate?.Longitude ?? 31.0);
            var sTags = (s.RequiredTagsCsv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (target == MatchEntityType.Factory)
            {
                var factories = await uow.Repository<Factory>().ListAsync(
                    f => f.IsPublic,
                    ct,
                    f => f.Governorate,
                    f => f.Machines,
                    f => f.IdleWindows,
                    f => f.CapabilityProfile!);

                var currentMonth = DateTime.UtcNow.Month;

                foreach (var f in factories.Take(50))
                {
                    var fGeo = new GeoPoint(f.Latitude, f.Longitude);
                    var fTags = (f.CapabilityProfile?.TagsCsv ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var hasIdle = f.IdleWindows.Any(w => w.StartMonth <= currentMonth && currentMonth <= w.EndMonth);
                    var score = RuleBasedScorer.StartupToFactory(sTags, sGeo, fTags, fGeo, f.UtilizationPercent, hasIdle);
                    var dist = Geo.HaversineKm(sGeo.Lat, sGeo.Lng, f.Latitude, f.Longitude);
                    var overlap = sTags.Count(fTags.Contains);

                    scoredCandidates.Add(new PreScoredCandidate(
                        f.Id,
                        f.Name,
                        $"{f.Sector} · {f.Machines.Count} machines",
                        f.Governorate?.NameEn ?? "Egypt",
                        dist,
                        score,
                        $"{overlap} required capability tags matched · {dist:N0} km away",
                        f.OwnerUserId));
                }
            }
            else if (target == MatchEntityType.Investor)
            {
                var investors = await uow.Repository<InvestorProfile>().ListAsync(null, ct);

                foreach (var inv in investors.Take(50))
                {
                    var invSectors = (inv.SectorsCsv ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(sec => Enum.TryParse<Sector>(sec, out var sc) ? sc : (Sector?)null)
                        .Where(sec => sec.HasValue).Select(sec => sec!.Value).ToHashSet();

                    var invStages = (inv.PreferredStagesCsv ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(stg => Enum.TryParse<StartupStage>(stg, out var st) ? st : (StartupStage?)null)
                        .Where(stg => stg.HasValue).Select(stg => stg!.Value).ToHashSet();

                    var score = RuleBasedScorer.StartupToInvestor(s.Sector, s.Stage, s.FundingNeededEgp, invSectors, invStages, inv.MinTicketEgp, inv.MaxTicketEgp);

                    scoredCandidates.Add(new PreScoredCandidate(
                        inv.Id,
                        inv.DisplayName,
                        inv.Bio ?? "Investor Profile",
                        "Egypt",
                        0,
                        score,
                        $"Focus: {s.Sector} · Stage: {s.Stage}",
                        inv.OwnerUserId));
                }
            }
        }

        // Sort by RuleScore descending and take top 8 candidates for AI re-ranking
        var topCandidates = scoredCandidates
            .OrderByDescending(c => c.RuleScore)
            .Take(8)
            .ToList();

        if (topCandidates.Count == 0)
        {
            return Result<MatchingSummaryDto>.Ok(new MatchingSummaryDto(
                source, sourceId, sourceTitle, target, [], false, DateTime.UtcNow));
        }

        // AI Re-ranking (with fail-soft fallback)
        var candidateMap = topCandidates.ToDictionary(c => c.Id);
        bool aiUsed = false;
        int? aiRunId = null;
        Dictionary<int, MatchRerankItem> aiResults = [];

        try
        {
            var call = new AiJsonCall(
                AiFeature.Matching,
                user.UserId,
                source.ToString(),
                sourceId,
                PromptCommon.System(opts.Value.OutputLanguage),
                MatchingPrompt.User(new { type = source.ToString(), id = sourceId, title = sourceTitle },
                                    topCandidates.Select(c => new { candidateId = c.Id, title = c.Title, ruleScore = c.RuleScore, location = c.Location, distanceKm = c.DistanceKm, subtitle = c.Subtitle })),
                MatchingPrompt.Version);

            var run = await ai.RunAsync<MatchRerankResult>(call, ct);
            if (run.Succeeded && run.Value?.Data?.Matches is { Count: > 0 })
            {
                aiUsed = true;
                aiRunId = run.Value.RunId;
                foreach (var item in run.Value.Data.Matches)
                {
                    // Discard unknown candidateId from AI
                    if (candidateMap.ContainsKey(item.CandidateId))
                    {
                        aiResults[item.CandidateId] = item;
                    }
                }
            }
        }
        catch
        {
            // Fail-soft: keep rule scores only
            aiUsed = false;
        }

        // Merge AI adjustments and rule scores
        var finalMatches = new List<MatchCandidateDto>();

        foreach (var c in topCandidates)
        {
            double adjustment = 0;
            List<string> reasons = [c.DefaultReason];
            string? nextStep = "Request connection to explore collaboration details.";

            if (aiResults.TryGetValue(c.Id, out var aiItem))
            {
                // Clamp adjustment to -15..+15
                adjustment = Math.Clamp(aiItem.Adjustment, -15, 15);
                if (aiItem.Reasons.Count > 0)
                {
                    reasons = aiItem.Reasons.Take(3).Select(r => r.Length > 200 ? r[..200] : r).ToList();
                }
                if (!string.IsNullOrWhiteSpace(aiItem.NextStep))
                {
                    nextStep = aiItem.NextStep.Trim();
                }
            }

            var finalScore = Math.Clamp(Math.Round(c.RuleScore + adjustment, 1), 0.0, 100.0);

            finalMatches.Add(new MatchCandidateDto(
                c.Id,
                c.Title,
                c.Subtitle,
                c.Location,
                c.DistanceKm,
                c.RuleScore,
                finalScore,
                reasons,
                nextStep,
                c.OwnerUserId,
                aiUsed && aiResults.ContainsKey(c.Id)));
        }

        // Top 5 final results
        var top5 = finalMatches
            .OrderByDescending(m => m.FinalScore)
            .Take(5)
            .ToList();

        // Persist MatchResult in database
        foreach (var m in top5)
        {
            await uow.Repository<MatchResult>().AddAsync(new MatchResult
            {
                RequestedByUserId = user.UserId,
                SourceType = source,
                SourceId = sourceId,
                TargetType = target,
                TargetId = m.Id,
                RuleScore = m.RuleScore,
                AiAdjustment = Math.Round(m.FinalScore - m.RuleScore, 1),
                FinalScore = m.FinalScore,
                ReasonsJson = JsonSerializer.Serialize(m.Reasons, JsonExtractor.Options),
                NextStep = m.NextStep,
                AiRunId = aiRunId
            }, ct);
        }

        await uow.SaveChangesAsync(ct);

        return Result<MatchingSummaryDto>.Ok(new MatchingSummaryDto(
            source,
            sourceId,
            sourceTitle,
            target,
            top5,
            aiUsed,
            DateTime.UtcNow));
    }

    private sealed record PreScoredCandidate(
        int Id,
        string Title,
        string Subtitle,
        string Location,
        double DistanceKm,
        double RuleScore,
        string DefaultReason,
        string OwnerUserId);
}
