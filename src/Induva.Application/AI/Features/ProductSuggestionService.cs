using System.Text.Json;
using Induva.Application.Abstractions;
using Induva.Application.AI.Prompts;
using Induva.Application.AI.Results;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Induva.Application.AI.Features;

public interface IProductSuggestionService
{
    Task<Result<ProductSuggestionSummaryDto>> SuggestAsync(int factoryId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSuggestionItemDto>> GetSuggestionsAsync(int factoryId, CancellationToken ct = default);
    Task<Result> SaveSuggestionAsync(int suggestionId, CancellationToken ct = default);
    Task<Result> DismissSuggestionAsync(int suggestionId, CancellationToken ct = default);
}

public sealed class ProductSuggestionService(
    IUnitOfWork uow,
    ICurrentUserService user,
    IAiJsonRunner ai,
    IOptions<AiOptions> opts) : IProductSuggestionService
{
    public async Task<Result<ProductSuggestionSummaryDto>> SuggestAsync(int factoryId, CancellationToken ct = default)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(
            factoryId,
            ct,
            x => x.Governorate,
            x => x.Machines,
            x => x.IdleWindows,
            x => x.Products,
            x => x.CapabilityProfile!);

        if (f is null)
            return Result<ProductSuggestionSummaryDto>.Fail(ErrorKind.NotFound, "Factory not found.");

        if (string.IsNullOrWhiteSpace(user.UserId) || (f.OwnerUserId != user.UserId && !user.IsInRole(Roles.Admin)))
            return Result<ProductSuggestionSummaryDto>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        if (f.CapabilityProfile is null)
            return Result<ProductSuggestionSummaryDto>.Fail(ErrorKind.Validation, "Run the factory analysis first.");

        // Grounding 1: Local raw materials in the factory's governorate
        var govResources = await uow.Repository<GovernorateResource>().ListAsync(
            r => r.GovernorateId == f.GovernorateId,
            ct,
            r => r.RawMaterial);

        var localRawMaterials = govResources
            .Select(r => r.RawMaterial?.NameEn ?? "")
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        // Grounding 2: Open demand requests overlapping with factory capability tags
        var factoryTags = (f.CapabilityProfile.TagsCsv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var openDemands = await uow.Repository<DemandRequest>().ListAsync(d => d.Status == "Open", ct);
        var matchingDemands = openDemands
            .Where(d => (d.RequiredTagsCsv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(tag => factoryTags.Contains(tag)))
            .Take(10)
            .ToList();

        var call = new AiJsonCall(
            AiFeature.ProductSuggestion,
            user.UserId,
            nameof(Factory),
            f.Id,
            PromptCommon.System(opts.Value.OutputLanguage),
            ProductSuggestionPrompt.User(f, localRawMaterials, matchingDemands),
            ProductSuggestionPrompt.Version);

        var run = await ai.RunAsync<ProductSuggestionResult>(call, ct);
        if (!run.Succeeded)
            return Result<ProductSuggestionSummaryDto>.Fail(run.Kind, run.Error ?? "Product suggestions failed.");

        var r = run.Value!.Data;

        // Factory machine names for subset check
        var machineNames = f.Machines.Select(m => m.Name.Trim()).ToList();

        // Server Post-processing:
        // 1. Drop items with empty name, estUnitPriceEgp <= 0, or estUnitCostEgp < 0. Keep <= 5.
        var validSuggestions = r.Suggestions
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.EstUnitPriceEgp > 0 && s.EstUnitCostEgp >= 0)
            .Take(5)
            .ToList();

        var processedEntities = new List<ProductSuggestion>();
        var itemDtos = new List<ProductSuggestionItemDto>();

        foreach (var s in validSuggestions)
        {
            var risks = new List<string>(s.Risks ?? []);

            // 2. Machine subset check (case-insensitive contains)
            foreach (var reqMach in s.RequiredMachines)
            {
                if (string.IsNullOrWhiteSpace(reqMach)) continue;
                var found = machineNames.Any(m => m.Contains(reqMach.Trim(), StringComparison.OrdinalIgnoreCase)
                                               || reqMach.Contains(m, StringComparison.OrdinalIgnoreCase));
                if (!found && !risks.Any(rk => rk.Contains("not listed", StringComparison.OrdinalIgnoreCase)))
                {
                    risks.Add("May need equipment not listed");
                }
            }

            // 3. Compute MarginPercent = (price - cost) / price * 100 in C#
            var marginPercent = s.EstUnitPriceEgp > 0
                ? Math.Round(((s.EstUnitPriceEgp - s.EstUnitCostEgp) / s.EstUnitPriceEgp) * 100, 1)
                : 0m;

            if (marginPercent < 5 && !risks.Contains("Thin margin"))
                risks.Add("Thin margin");
            else if (marginPercent > 70 && !risks.Any(rk => rk.Contains("unusually high", StringComparison.OrdinalIgnoreCase)))
                risks.Add("Margin looks unusually high — verify costs");

            // 4. Clamp confidence
            var confidence = Math.Clamp(s.Confidence, 0.0, 1.0);

            // 5. Controlled vocabulary tags
            var tags = s.RequiredCapabilityTags
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(CapabilityTags.IsValid)
                .Distinct()
                .ToList();

            var entity = new ProductSuggestion
            {
                FactoryId = f.Id,
                AiRunId = run.Value.RunId,
                Name = s.Name.Trim(),
                Category = s.Category?.Trim(),
                Rationale = s.Rationale.Trim(),
                RequiredMachinesCsv = string.Join(',', s.RequiredMachines),
                RequiredMaterialsCsv = string.Join(',', s.RequiredRawMaterials),
                EstUnitCostEgp = s.EstUnitCostEgp,
                EstUnitPriceEgp = s.EstUnitPriceEgp,
                EstMonthlyVolume = s.EstMonthlyVolume,
                DemandLevel = s.DemandLevel,
                SeasonalityNote = s.SeasonalityNote?.Trim(),
                Confidence = confidence,
                RisksJson = JsonSerializer.Serialize(risks, JsonExtractor.Options),
                Status = SuggestionStatus.Suggested
            };

            processedEntities.Add(entity);
        }

        // Database persistence:
        // Replace factory's previous suggestions with Status == Suggested (preserve Saved!)
        var prevSuggestions = await uow.Repository<ProductSuggestion>().ListAsync(
            ps => ps.FactoryId == factoryId && ps.Status == SuggestionStatus.Suggested,
            ct);

        foreach (var old in prevSuggestions)
        {
            var tracked = await uow.Repository<ProductSuggestion>().GetByIdAsync(old.Id, ct) ?? old;
            uow.Repository<ProductSuggestion>().Remove(tracked);
        }

        foreach (var entity in processedEntities)
        {
            await uow.Repository<ProductSuggestion>().AddAsync(entity, ct);
        }

        await uow.SaveChangesAsync(ct);

        // Fetch all active (Suggested + Saved) suggestions for the factory
        var allActive = await GetSuggestionsAsync(factoryId, ct);

        return Result<ProductSuggestionSummaryDto>.Ok(new ProductSuggestionSummaryDto(
            f.Id,
            f.Name,
            allActive,
            r.Assumptions,
            run.Value.FromCache,
            DateTime.UtcNow));
    }

    public async Task<IReadOnlyList<ProductSuggestionItemDto>> GetSuggestionsAsync(int factoryId, CancellationToken ct = default)
    {
        var list = await uow.Repository<ProductSuggestion>().ListAsync(
            ps => ps.FactoryId == factoryId && ps.Status != SuggestionStatus.Dismissed,
            ct);

        return list.OrderBy(s => s.Status == SuggestionStatus.Saved ? 0 : 1)
                   .ThenByDescending(s => s.Confidence)
                   .Select(s =>
                   {
                       var margin = s.EstUnitPriceEgp > 0
                           ? Math.Round(((s.EstUnitPriceEgp - s.EstUnitCostEgp) / s.EstUnitPriceEgp) * 100, 1)
                           : 0m;

                       List<string> risks;
                       try
                       {
                           risks = JsonSerializer.Deserialize<List<string>>(s.RisksJson, JsonExtractor.Options) ?? [];
                       }
                       catch
                       {
                           risks = [];
                       }

                       var machines = s.RequiredMachinesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                       var materials = s.RequiredMaterialsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                       return new ProductSuggestionItemDto(
                           s.Id,
                           s.Name,
                           s.Category,
                           s.Rationale,
                           machines,
                           materials,
                           [],
                           s.EstUnitCostEgp,
                           s.EstUnitPriceEgp,
                           s.EstMonthlyVolume,
                           margin,
                           s.DemandLevel,
                           s.SeasonalityNote,
                           s.Confidence,
                           risks,
                           s.Status);
                   }).ToList();
    }

    public async Task<Result> SaveSuggestionAsync(int suggestionId, CancellationToken ct = default)
    {
        var s = await uow.Repository<ProductSuggestion>().GetByIdAsync(suggestionId, ct, ps => ps.Factory);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Suggestion not found.");

        if (string.IsNullOrWhiteSpace(user.UserId) || (s.Factory?.OwnerUserId != user.UserId && !user.IsInRole(Roles.Admin)))
            return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        s.Status = SuggestionStatus.Saved;
        uow.Repository<ProductSuggestion>().Update(s);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DismissSuggestionAsync(int suggestionId, CancellationToken ct = default)
    {
        var s = await uow.Repository<ProductSuggestion>().GetByIdAsync(suggestionId, ct, ps => ps.Factory);
        if (s is null) return Result.Fail(ErrorKind.NotFound, "Suggestion not found.");

        if (string.IsNullOrWhiteSpace(user.UserId) || (s.Factory?.OwnerUserId != user.UserId && !user.IsInRole(Roles.Admin)))
            return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        s.Status = SuggestionStatus.Dismissed;
        uow.Repository<ProductSuggestion>().Update(s);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
