namespace Induva.Application.AI.Finance;

public sealed record InvestmentAssumptions(
    decimal CapexEgp,
    decimal MonthlyVolumeUnits,
    decimal UnitPriceEgp,
    decimal UnitMaterialCostEgp,
    decimal UnitLaborCostEgp,
    decimal UnitEnergyCostEgp,
    decimal UnitTransportCostEgp,
    decimal MonthlyFixedCostsEgp,
    int RampUpMonths)
{
    public decimal UnitVariableCost => UnitMaterialCostEgp + UnitLaborCostEgp + UnitEnergyCostEgp + UnitTransportCostEgp;
}

public sealed record InvestmentMetrics(
    decimal UnitMarginEgp,
    decimal MarginPercent,
    decimal? BreakEvenUnitsPerMonth,
    int? PaybackMonths,
    decimal TotalOperatingProfit,
    decimal? RoiPercent,
    decimal Npv,
    IReadOnlyList<decimal> CumulativeCashFlow);

public sealed record ScenarioResult(string Name, InvestmentMetrics Metrics);

public static class FinancialCalculator
{
    public static InvestmentMetrics Calculate(InvestmentAssumptions a, int years = 3, decimal annualDiscountRate = 0.20m)
    {
        var months = Math.Clamp(years, 1, 10) * 12;
        var unitMargin = a.UnitPriceEgp - a.UnitVariableCost;
        var marginPct = a.UnitPriceEgp > 0 ? (unitMargin / a.UnitPriceEgp) * 100 : 0;
        decimal? breakEven = unitMargin > 0 ? a.MonthlyFixedCostsEgp / unitMargin : null;

        var monthlyRate = Math.Pow(1 + (double)annualDiscountRate, 1.0 / 12) - 1;
        var cumulative = -a.CapexEgp;
        decimal totalProfit = 0;
        var npv = -a.CapexEgp;
        int? payback = null;
        var series = new List<decimal>(months);

        for (var t = 1; t <= months; t++)
        {
            var ramp = a.RampUpMonths <= 0 ? 1m : Math.Min(1m, (decimal)t / a.RampUpMonths);
            var profit = (a.MonthlyVolumeUnits * ramp * unitMargin) - a.MonthlyFixedCostsEgp;
            totalProfit += profit;
            cumulative += profit;
            series.Add(Math.Round(cumulative, 2));

            npv += profit / (decimal)Math.Pow(1 + monthlyRate, t);
            if (payback is null && cumulative >= 0)
            {
                payback = t;
            }
        }

        decimal? roi = a.CapexEgp > 0 ? ((totalProfit - a.CapexEgp) / a.CapexEgp) * 100 : null;

        return new InvestmentMetrics(
            Math.Round(unitMargin, 2),
            Math.Round(marginPct, 1),
            breakEven.HasValue ? Math.Round(breakEven.Value, 0) : null,
            payback,
            Math.Round(totalProfit, 0),
            roi.HasValue ? Math.Round(roi.Value, 1) : null,
            Math.Round(npv, 0),
            series);
    }

    public static IReadOnlyList<ScenarioResult> Scenarios(InvestmentAssumptions a, int years = 3, decimal rate = 0.20m) =>
    [
        new("Conservative", Calculate(a with
        {
            MonthlyVolumeUnits = a.MonthlyVolumeUnits * 0.7m,
            UnitPriceEgp = a.UnitPriceEgp * 0.9m
        }, years, rate)),
        new("Base", Calculate(a, years, rate)),
        new("Optimistic", Calculate(a with
        {
            MonthlyVolumeUnits = a.MonthlyVolumeUnits * 1.2m,
            UnitPriceEgp = a.UnitPriceEgp * 1.05m
        }, years, rate))
    ];
}

