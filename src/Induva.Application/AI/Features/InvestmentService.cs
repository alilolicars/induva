using System.Text.Json;
using Induva.Application.Abstractions;
using Induva.Application.AI.Finance;
using Induva.Application.AI.Prompts;
using Induva.Application.AI.Results;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Induva.Application.AI.Features;

public interface IInvestmentService
{
    Task<Result<InvestmentAssumptionsDto>> GetAssumptionsAsync(GetInvestmentAssumptionsRequestDto req, CancellationToken ct = default);
    Task<Result<InvestmentAnalysisDto>> CalculateAsync(CalculateInvestmentRequestDto req, CancellationToken ct = default);
    Task<Result<InvestmentAnalysisDto>> GetByIdAsync(int id, CancellationToken ct = default);
}

public sealed class InvestmentService(
    IUnitOfWork uow,
    ICurrentUserService user,
    IAiJsonRunner ai,
    IOptions<AiOptions> opts) : IInvestmentService
{
    private const string StandardDisclaimer = "Indicative estimate generated from AI assumptions and a simplified model. Not financial advice. Verify all inputs with suppliers and a qualified advisor.";

    public async Task<Result<InvestmentAssumptionsDto>> GetAssumptionsAsync(GetInvestmentAssumptionsRequestDto req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ProductName))
            return Result<InvestmentAssumptionsDto>.Fail(ErrorKind.Validation, "Product name is required.");

        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<InvestmentAssumptionsDto>.Fail(ErrorKind.Forbidden, "Please log in.");

        // 1. Resolve Factory and Governorate
        Factory? factory = null;
        if (req.FactoryId.HasValue)
        {
            factory = await uow.Repository<Factory>().GetByIdAsync(
                req.FactoryId.Value,
                ct,
                f => f.Governorate,
                f => f.Machines);
        }

        var govId = req.GovernorateId ?? factory?.GovernorateId ?? 1;
        var gov = await uow.Repository<Governorate>().GetByIdAsync(govId, ct)
               ?? (await uow.Repository<Governorate>().ListAsync(null, ct)).FirstOrDefault();

        var govName = gov?.NameEn ?? "Cairo";
        var govLat = gov?.Latitude ?? 30.04;
        var govLng = gov?.Longitude ?? 31.23;

        // 2. Deterministic distance hint to nearest raw material source
        var allGovResources = await uow.Repository<GovernorateResource>().ListAsync(
            null,
            ct,
            r => r.Governorate,
            r => r.RawMaterial);

        double? nearestMaterialDistanceKm = null;
        if (allGovResources.Count > 0)
        {
            var pName = req.ProductName.ToLowerInvariant();
            var matches = allGovResources
                .Where(r => r.RawMaterial != null && (pName.Contains(r.RawMaterial.NameEn.ToLower()) || pName.Contains(r.RawMaterial.Category.ToLower())))
                .ToList();

            var pool = matches.Count > 0 ? matches : allGovResources;
            nearestMaterialDistanceKm = pool
                .Select(r => Geo.HaversineKm(govLat, govLng, r.Governorate?.Latitude ?? govLat, r.Governorate?.Longitude ?? govLng))
                .DefaultIfEmpty(25.0)
                .Min();
        }

        // 3. Existing factory summary
        string? factorySummary = null;
        if (factory is not null)
        {
            var machineList = string.Join(", ", factory.Machines.Select(m => m.Name));
            factorySummary = $"Existing factory: {factory.Name} with equipment: {machineList}";
        }

        var call = new AiJsonCall(
            AiFeature.Investment,
            user.UserId,
            nameof(InvestmentAnalysis),
            req.FactoryId ?? 0,
            PromptCommon.System(opts.Value.OutputLanguage),
            InvestmentPrompt.User(req.ProductName, req.Category, govName, factorySummary, req.DesiredMonthlyVolume, nearestMaterialDistanceKm),
            InvestmentPrompt.Version);

        var run = await ai.RunAsync<InvestmentAssumptionsResult>(call, ct);
        if (!run.Succeeded)
            return Result<InvestmentAssumptionsDto>.Fail(run.Kind, run.Error ?? "Failed to propose investment assumptions.");

        var r = run.Value!.Data;
        var a = r.Assumptions ?? new AssumptionValues();

        // Server Post-processing: Clamp values >= 0
        a.CapexEgp = Math.Max(0, a.CapexEgp);
        a.MonthlyVolumeUnits = Math.Max(0, a.MonthlyVolumeUnits);
        a.UnitPriceEgp = Math.Max(0, a.UnitPriceEgp);
        a.UnitMaterialCostEgp = Math.Max(0, a.UnitMaterialCostEgp);
        a.UnitLaborCostEgp = Math.Max(0, a.UnitLaborCostEgp);
        a.UnitEnergyCostEgp = Math.Max(0, a.UnitEnergyCostEgp);
        a.UnitTransportCostEgp = Math.Max(0, a.UnitTransportCostEgp);
        a.MonthlyFixedCostsEgp = Math.Max(0, a.MonthlyFixedCostsEgp);
        a.RampUpMonths = Math.Clamp(a.RampUpMonths, 0, 24);

        var dto = new InvestmentAssumptionsDto(
            req.ProductName.Trim(),
            req.Category?.Trim(),
            gov?.Id,
            govName,
            factory?.Id,
            factory?.Name,
            a,
            r.CapexBreakdown ?? [],
            r.Rationale ?? [],
            r.KeyRisks ?? [],
            Math.Clamp(r.Confidence, 0.0, 1.0),
            run.Value.FromCache);

        return Result<InvestmentAssumptionsDto>.Ok(dto);
    }

    public async Task<Result<InvestmentAnalysisDto>> CalculateAsync(CalculateInvestmentRequestDto req, CancellationToken ct = default)
    {
        // NO AI CALL - 100% deterministic calculation
        if (string.IsNullOrWhiteSpace(req.ProductName))
            return Result<InvestmentAnalysisDto>.Fail(ErrorKind.Validation, "Product name is required.");

        if (req.UnitPriceEgp <= 0)
            return Result<InvestmentAnalysisDto>.Fail(ErrorKind.Validation, "Unit price must be greater than zero.");

        if (req.MonthlyVolumeUnits <= 0)
            return Result<InvestmentAnalysisDto>.Fail(ErrorKind.Validation, "Monthly volume must be greater than zero.");

        if (req.CapexEgp < 0 || req.UnitMaterialCostEgp < 0 || req.UnitLaborCostEgp < 0 || req.UnitEnergyCostEgp < 0 ||
            req.UnitTransportCostEgp < 0 || req.MonthlyFixedCostsEgp < 0)
        {
            return Result<InvestmentAnalysisDto>.Fail(ErrorKind.Validation, "Costs and Capex cannot be negative.");
        }

        var rampUp = Math.Clamp(req.RampUpMonths, 0, 24);
        var years = Math.Clamp(req.Years, 1, 10);
        var discountRate = req.AnnualDiscountRate > 0 ? req.AnnualDiscountRate : 0.20m;

        var assumptions = new InvestmentAssumptions(
            req.CapexEgp,
            req.MonthlyVolumeUnits,
            req.UnitPriceEgp,
            req.UnitMaterialCostEgp,
            req.UnitLaborCostEgp,
            req.UnitEnergyCostEgp,
            req.UnitTransportCostEgp,
            req.MonthlyFixedCostsEgp,
            rampUp);

        // Run deterministic multi-scenario calculator
        var scenarios = FinancialCalculator.Scenarios(assumptions, years, discountRate);
        var baseScenario = scenarios.First(s => s.Name == "Base").Metrics;

        // Warnings evaluation
        var warnings = new List<string>();
        if (baseScenario.UnitMarginEgp <= 0)
            warnings.Add("⚠️ Negative or zero margin: Project loses money on every unit produced.");
        else if (baseScenario.MarginPercent > 70)
            warnings.Add("⚠️ Unusually high margin (> 70%): Verify unit variable costs and wholesale pricing.");

        if (req.CapexEgp == 0)
            warnings.Add("ℹ️ Capex is zero: Project utilizes existing machinery entirely; ROI percentage is not meaningful.");

        if (baseScenario.PaybackMonths is null || baseScenario.PaybackMonths > years * 12)
            warnings.Add($"⚠️ Payback not achieved within the {years}-year projection window.");

        // Resolve Governorate
        var govId = req.GovernorateId ?? 1;
        var gov = await uow.Repository<Governorate>().GetByIdAsync(govId, ct)
               ?? (await uow.Repository<Governorate>().ListAsync(null, ct)).FirstOrDefault();
        var govName = gov?.NameEn ?? "Egypt";

        // Persist InvestmentAnalysis
        var entity = new InvestmentAnalysis
        {
            RequestedByUserId = user.UserId ?? "anonymous",
            Title = $"Investment Analysis: {req.ProductName}",
            FactoryId = req.FactoryId,
            GovernorateId = gov?.Id ?? 1,
            PaybackMonths = baseScenario.PaybackMonths,
            ThreeYearRoi = baseScenario.RoiPercent,
            AssumptionsJson = JsonSerializer.Serialize(assumptions, JsonExtractor.Options),
            MetricsJson = JsonSerializer.Serialize(new
            {
                years,
                discountRate,
                scenarios,
                warnings,
                rationale = req.Rationale ?? [],
                keyRisks = req.KeyRisks ?? []
            }, JsonExtractor.Options)
        };

        await uow.Repository<InvestmentAnalysis>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);

        var dto = new InvestmentAnalysisDto(
            entity.Id,
            req.ProductName,
            req.Category,
            govName,
            req.FactoryId,
            null,
            assumptions,
            scenarios,
            warnings,
            req.Rationale ?? [],
            req.KeyRisks ?? [],
            StandardDisclaimer,
            DateTime.UtcNow);

        return Result<InvestmentAnalysisDto>.Ok(dto);
    }

    public async Task<Result<InvestmentAnalysisDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await uow.Repository<InvestmentAnalysis>().GetByIdAsync(
            id,
            ct,
            x => x.Governorate,
            x => x.Factory!);

        if (entity is null)
            return Result<InvestmentAnalysisDto>.Fail(ErrorKind.NotFound, "Investment analysis not found.");

        try
        {
            var assumptions = JsonSerializer.Deserialize<InvestmentAssumptions>(entity.AssumptionsJson, JsonExtractor.Options);
            using var doc = JsonDocument.Parse(entity.MetricsJson);
            var root = doc.RootElement;

            var scenarios = JsonSerializer.Deserialize<List<ScenarioResult>>(root.GetProperty("scenarios").GetRawText(), JsonExtractor.Options) ?? [];
            var warnings = JsonSerializer.Deserialize<List<string>>(root.GetProperty("warnings").GetRawText(), JsonExtractor.Options) ?? [];
            var rationale = JsonSerializer.Deserialize<List<string>>(root.GetProperty("rationale").GetRawText(), JsonExtractor.Options) ?? [];
            var keyRisks = JsonSerializer.Deserialize<List<string>>(root.GetProperty("keyRisks").GetRawText(), JsonExtractor.Options) ?? [];

            var dto = new InvestmentAnalysisDto(
                entity.Id,
                entity.Title.Replace("Investment Analysis: ", ""),
                null,
                entity.Governorate?.NameEn ?? "Egypt",
                entity.FactoryId,
                entity.Factory?.Name,
                assumptions!,
                scenarios,
                warnings,
                rationale,
                keyRisks,
                StandardDisclaimer,
                entity.CreatedAtUtc);

            return Result<InvestmentAnalysisDto>.Ok(dto);
        }
        catch
        {
            return Result<InvestmentAnalysisDto>.Fail(ErrorKind.NotFound, "Failed to parse investment analysis data.");
        }
    }
}

