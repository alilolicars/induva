using Induva.Application.AI.Finance;
using Xunit;

namespace Induva.Tests;

public class FinancialCalculatorTests
{
    [Fact]
    public void Unit_margin_and_margin_percent_are_computed_correctly()
    {
        // Price: 100 EGP, Variable cost: 60 EGP (40 mat + 10 labor + 5 energy + 5 trans)
        var a = new InvestmentAssumptions(
            CapexEgp: 100000,
            MonthlyVolumeUnits: 1000,
            UnitPriceEgp: 100,
            UnitMaterialCostEgp: 40,
            UnitLaborCostEgp: 10,
            UnitEnergyCostEgp: 5,
            UnitTransportCostEgp: 5,
            MonthlyFixedCostsEgp: 10000,
            RampUpMonths: 1);

        Assert.Equal(60m, a.UnitVariableCost);

        var metrics = FinancialCalculator.Calculate(a, years: 1);

        Assert.Equal(40m, metrics.UnitMarginEgp);
        Assert.Equal(40.0m, metrics.MarginPercent);
        // Break-even: Fixed (10,000) / Margin (40) = 250 units
        Assert.Equal(250m, metrics.BreakEvenUnitsPerMonth);
    }

    [Fact]
    public void Hand_computed_scenario_verifies_payback_and_profit()
    {
        // Simple 1-year test with zero ramp-up and zero fixed cost
        // Capex: 120,000 EGP
        // Monthly volume: 1,000 units
        // Unit margin: 20 EGP (Price 50 - Variable 30)
        // Monthly operating profit = 1,000 * 20 = 20,000 EGP
        // Cumulative cash flow:
        // Month 1: -120k + 20k = -100k
        // Month 2: -80k
        // Month 3: -60k
        // Month 4: -40k
        // Month 5: -20k
        // Month 6: 0 -> Payback exactly at Month 6!
        // Total 1-year operating profit: 12 * 20,000 = 240,000 EGP
        // ROI: (240k - 120k) / 120k * 100 = 100%
        var a = new InvestmentAssumptions(
            CapexEgp: 120000,
            MonthlyVolumeUnits: 1000,
            UnitPriceEgp: 50,
            UnitMaterialCostEgp: 20,
            UnitLaborCostEgp: 5,
            UnitEnergyCostEgp: 3,
            UnitTransportCostEgp: 2,
            MonthlyFixedCostsEgp: 0,
            RampUpMonths: 0);

        var metrics = FinancialCalculator.Calculate(a, years: 1, annualDiscountRate: 0.0m);

        Assert.Equal(6, metrics.PaybackMonths);
        Assert.Equal(240000m, metrics.TotalOperatingProfit);
        Assert.Equal(100.0m, metrics.RoiPercent);
        Assert.Equal(120000m, metrics.Npv); // 240k profit - 120k capex at 0% discount
    }

    [Fact]
    public void Scenarios_generates_three_expected_cases()
    {
        var a = new InvestmentAssumptions(
            CapexEgp: 500000,
            MonthlyVolumeUnits: 5000,
            UnitPriceEgp: 80,
            UnitMaterialCostEgp: 45,
            UnitLaborCostEgp: 8,
            UnitEnergyCostEgp: 4,
            UnitTransportCostEgp: 3,
            MonthlyFixedCostsEgp: 30000,
            RampUpMonths: 3);

        var scenarios = FinancialCalculator.Scenarios(a, years: 3);

        Assert.Equal(3, scenarios.Count);
        Assert.Contains(scenarios, s => s.Name == "Conservative");
        Assert.Contains(scenarios, s => s.Name == "Base");
        Assert.Contains(scenarios, s => s.Name == "Optimistic");

        var cons = scenarios.First(s => s.Name == "Conservative");
        var opt = scenarios.First(s => s.Name == "Optimistic");

        // Optimistic NPV should be higher than Conservative NPV
        Assert.True(opt.Metrics.Npv > cons.Metrics.Npv);
    }
}

