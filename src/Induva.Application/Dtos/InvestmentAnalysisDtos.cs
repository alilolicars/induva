using Induva.Application.AI.Finance;
using Induva.Application.AI.Results;

namespace Induva.Application.Dtos;

public sealed record GetInvestmentAssumptionsRequestDto(
    string ProductName,
    string? Category = null,
    int? GovernorateId = null,
    int? FactoryId = null,
    decimal? DesiredMonthlyVolume = null);

public sealed record InvestmentAssumptionsDto(
    string ProductName,
    string? Category,
    int? GovernorateId,
    string? GovernorateName,
    int? FactoryId,
    string? FactoryName,
    AssumptionValues Assumptions,
    IReadOnlyList<CapexItem> CapexBreakdown,
    IReadOnlyList<string> Rationale,
    IReadOnlyList<string> KeyRisks,
    double Confidence,
    bool FromCache);

public sealed record CalculateInvestmentRequestDto(
    string ProductName,
    string? Category,
    int? GovernorateId,
    int? FactoryId,
    decimal CapexEgp,
    decimal MonthlyVolumeUnits,
    decimal UnitPriceEgp,
    decimal UnitMaterialCostEgp,
    decimal UnitLaborCostEgp,
    decimal UnitEnergyCostEgp,
    decimal UnitTransportCostEgp,
    decimal MonthlyFixedCostsEgp,
    int RampUpMonths,
    int Years = 3,
    decimal AnnualDiscountRate = 0.20m,
    IReadOnlyList<string>? Rationale = null,
    IReadOnlyList<string>? KeyRisks = null);

public sealed record InvestmentAnalysisDto(
    int Id,
    string ProductName,
    string? Category,
    string GovernorateName,
    int? FactoryId,
    string? FactoryName,
    InvestmentAssumptions Assumptions,
    IReadOnlyList<ScenarioResult> Scenarios,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Rationale,
    IReadOnlyList<string> KeyRisks,
    string Disclaimer,
    DateTime CreatedAtUtc);

