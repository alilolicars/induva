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

public interface IFactoryAnalysisService
{
    Task<Result<FactoryAnalysisDto>> AnalyzeAsync(int factoryId, CancellationToken ct = default);
}

public sealed class FactoryAnalysisService(
    IUnitOfWork uow,
    ICurrentUserService user,
    IAiJsonRunner ai,
    IOptions<AiOptions> opts) : IFactoryAnalysisService
{
    public async Task<Result<FactoryAnalysisDto>> AnalyzeAsync(int factoryId, CancellationToken ct = default)
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
            return Result<FactoryAnalysisDto>.Fail(ErrorKind.NotFound, "Factory not found.");

        if (string.IsNullOrWhiteSpace(user.UserId) || (f.OwnerUserId != user.UserId && !user.IsInRole(Roles.Admin)))
            return Result<FactoryAnalysisDto>.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        if (f.Machines.Count == 0)
            return Result<FactoryAnalysisDto>.Fail(ErrorKind.Validation, "Add at least one machine before running the analysis.");

        if (string.IsNullOrWhiteSpace(f.Description) && f.Products.Count == 0)
            return Result<FactoryAnalysisDto>.Fail(ErrorKind.Validation, "Add a description or your current products first.");

        var call = new AiJsonCall(
            AiFeature.FactoryAnalysis,
            user.UserId,
            nameof(Factory),
            f.Id,
            PromptCommon.System(opts.Value.OutputLanguage),
            FactoryAnalysisPrompt.User(f),
            FactoryAnalysisPrompt.Version);

        var run = await ai.RunAsync<FactoryAnalysisResult>(call, ct);
        if (!run.Succeeded)
            return Result<FactoryAnalysisDto>.Fail(run.Kind, run.Error ?? "AI analysis failed.");

        var r = run.Value!.Data;

        // Controlled vocabulary: normalize tags against CapabilityTags.All
        var tags = r.CapabilityTags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => CapabilityTags.IsValid(t))
            .Distinct()
            .Take(12)
            .ToList();

        var profile = f.CapabilityProfile ?? new FactoryCapabilityProfile { FactoryId = f.Id };
        profile.Summary = r.Summary.Length > 600 ? r.Summary[..600] : r.Summary;
        profile.TagsCsv = string.Join(',', tags);
        profile.ResultJson = JsonSerializer.Serialize(r, JsonExtractor.Options);
        profile.AiRunId = run.Value.RunId;
        profile.GeneratedAtUtc = DateTime.UtcNow;

        if (f.CapabilityProfile is null)
            await uow.Repository<FactoryCapabilityProfile>().AddAsync(profile, ct);
        else
            uow.Repository<FactoryCapabilityProfile>().Update(profile);

        await uow.SaveChangesAsync(ct);

        var dto = new FactoryAnalysisDto(
            profile.Summary,
            tags,
            r.Processes,
            r.Strengths,
            r.Constraints,
            r.UnderusedAssets,
            r.MissingInformation,
            Math.Clamp(r.Confidence, 0, 1),
            profile.GeneratedAtUtc,
            run.Value.FromCache,
            Warning: tags.Count == 0 ? "No recognizable capabilities were found. Add more specific machine details." : null);

        return Result<FactoryAnalysisDto>.Ok(dto);
    }
}

