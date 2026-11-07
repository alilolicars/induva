using System.Text.Json;
using Induva.Application.Abstractions;
using Induva.Application.AI.Prompts;
using Induva.Application.AI.Results;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Induva.Application.AI.Features;

public interface IDemandForecastService
{
    Task<Result<DemandForecastDto>> ForecastAsync(GenerateDemandForecastDto req, CancellationToken ct = default);
    Task<Result<DemandForecastDto>> GetLatestForecastAsync(string productName, int? governorateId = null, CancellationToken ct = default);
}

public sealed class DemandForecastService(
    IUnitOfWork uow,
    ICurrentUserService user,
    IAiJsonRunner ai,
    IOptions<AiOptions> opts) : IDemandForecastService
{
    public async Task<Result<DemandForecastDto>> ForecastAsync(GenerateDemandForecastDto req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ProductName))
            return Result<DemandForecastDto>.Fail(ErrorKind.Validation, "Product name is required for forecasting.");

        var horizon = req.HorizonMonths == 6 ? 6 : 12;

        string? governorateName = null;
        if (req.GovernorateId.HasValue)
        {
            var g = await uow.Repository<Governorate>().GetByIdAsync(req.GovernorateId.Value, ct);
            governorateName = g?.NameEn;
        }

        // Server assigns month labels starting from next calendar month
        var start = DateTime.UtcNow.AddMonths(1);
        var monthLabels = new List<string>();
        for (var i = 0; i < horizon; i++)
        {
            monthLabels.Add(start.AddMonths(i).ToString("MMM yyyy"));
        }
        var startingMonthName = start.ToString("MMMM yyyy");

        // Grounding: platform historical demand stats over last 12 months
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var pName = req.ProductName.Trim().ToLowerInvariant();

        var openDemands = await uow.Repository<DemandRequest>().ListAsync(
            d => (d.ProductName.ToLower().Contains(pName) ||
                 (req.Category != null && d.Sector.ToString() == req.Category)) &&
                 d.CreatedAtUtc >= twelveMonthsAgo,
            ct);

        var platformStats = openDemands
            .GroupBy(d => d.CreatedAtUtc.Month)
            .Select(g => (Month: g.Key, RequestCount: g.Count(), TotalQuantity: g.Sum(x => x.Quantity)))
            .ToList();

        var call = new AiJsonCall(
            AiFeature.DemandForecast,
            user.UserId ?? "public-user",
            nameof(DemandForecast),
            0,
            PromptCommon.System(opts.Value.OutputLanguage),
            DemandForecastPrompt.User(req.ProductName, req.Category, governorateName, horizon, startingMonthName, platformStats),
            DemandForecastPrompt.Version);

        var run = await ai.RunAsync<DemandForecastResult>(call, ct);
        if (!run.Succeeded)
            return Result<DemandForecastDto>.Fail(run.Kind, run.Error ?? "Demand forecast failed.");

        var r = run.Value!.Data;

        // Post-processing:
        // 1. Length check: if MonthlyIndex.Count != N -> pad or trim, set confidence <= 0.3, add assumption
        var monthlyIndex = new List<double>(r.MonthlyIndex ?? []);
        var confidence = Math.Clamp(r.Confidence, 0.0, 1.0);
        var assumptions = new List<string>(r.Assumptions ?? []);

        if (monthlyIndex.Count != horizon)
        {
            confidence = Math.Min(confidence, 0.3);
            if (!assumptions.Contains("Forecast length adjusted"))
                assumptions.Add("Forecast length adjusted");

            if (monthlyIndex.Count > horizon)
            {
                monthlyIndex = monthlyIndex.Take(horizon).ToList();
            }
            else
            {
                var lastVal = monthlyIndex.Count > 0 ? monthlyIndex[^1] : 50.0;
                while (monthlyIndex.Count < horizon)
                {
                    monthlyIndex.Add(lastVal);
                }
            }
        }

        // 2. Clamp each value 0..100
        monthlyIndex = monthlyIndex.Select(v => Math.Clamp(Math.Round(v, 1), 0.0, 100.0)).ToList();

        // 3. Allowed Trend: Rising | Stable | Falling
        var trend = r.Trend switch
        {
            "Rising" or "Falling" or "Stable" => r.Trend,
            _ => "Stable"
        };

        // 4. Allowed DataBasis: PlatformData | GeneralKnowledge | Mixed
        var dataBasis = r.DataBasis switch
        {
            "PlatformData" or "GeneralKnowledge" or "Mixed" => r.DataBasis,
            _ => "GeneralKnowledge"
        };

        var peakMonths = (r.PeakMonths ?? [])
            .Where(m => m is >= 1 and <= 12)
            .Distinct()
            .ToList();

        var drivers = r.Drivers ?? [];

        // Database persistence
        var forecastEntity = new DemandForecast
        {
            RequestedByUserId = user.UserId ?? "anonymous",
            ProductName = req.ProductName.Trim(),
            Category = req.Category?.Trim(),
            GovernorateId = req.GovernorateId,
            HorizonMonths = horizon,
            Trend = trend,
            Confidence = confidence,
            AiRunId = run.Value.RunId,
            ResultJson = JsonSerializer.Serialize(new
            {
                monthLabels,
                monthlyIndex,
                peakMonths,
                drivers,
                assumptions,
                dataBasis,
                confidence
            }, JsonExtractor.Options)
        };

        await uow.Repository<DemandForecast>().AddAsync(forecastEntity, ct);
        await uow.SaveChangesAsync(ct);

        var dto = new DemandForecastDto(
            forecastEntity.Id,
            forecastEntity.ProductName,
            forecastEntity.Category,
            governorateName,
            horizon,
            trend,
            monthLabels,
            monthlyIndex,
            peakMonths,
            drivers,
            assumptions,
            dataBasis,
            confidence,
            DateTime.UtcNow,
            run.Value.FromCache);

        return Result<DemandForecastDto>.Ok(dto);
    }

    public async Task<Result<DemandForecastDto>> GetLatestForecastAsync(string productName, int? governorateId = null, CancellationToken ct = default)
    {
        var pName = productName.Trim().ToLowerInvariant();
        var forecasts = await uow.Repository<DemandForecast>().ListAsync(
            f => f.ProductName.ToLower() == pName && (governorateId == null || f.GovernorateId == governorateId),
            ct,
            f => f.Governorate!);

        var latest = forecasts.OrderByDescending(f => f.CreatedAtUtc).FirstOrDefault();
        if (latest is null)
            return Result<DemandForecastDto>.Fail(ErrorKind.NotFound, "No forecast found for this product.");

        try
        {
            using var doc = JsonDocument.Parse(latest.ResultJson);
            var root = doc.RootElement;

            var monthLabels = root.GetProperty("monthLabels").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            var monthlyIndex = root.GetProperty("monthlyIndex").EnumerateArray().Select(x => x.GetDouble()).ToList();
            var peakMonths = root.GetProperty("peakMonths").EnumerateArray().Select(x => x.GetInt32()).ToList();
            var drivers = root.GetProperty("drivers").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            var assumptions = root.GetProperty("assumptions").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            var dataBasis = root.GetProperty("dataBasis").GetString() ?? "GeneralKnowledge";

            return Result<DemandForecastDto>.Ok(new DemandForecastDto(
                latest.Id,
                latest.ProductName,
                latest.Category,
                latest.Governorate?.NameEn,
                latest.HorizonMonths,
                latest.Trend,
                monthLabels,
                monthlyIndex,
                peakMonths,
                drivers,
                assumptions,
                dataBasis,
                latest.Confidence,
                latest.CreatedAtUtc,
                FromCache: false));
        }
        catch
        {
            return Result<DemandForecastDto>.Fail(ErrorKind.NotFound, "Failed to parse saved forecast.");
        }
    }
}
