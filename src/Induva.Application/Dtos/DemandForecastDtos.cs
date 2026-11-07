namespace Induva.Application.Dtos;

public sealed record DemandForecastDto(
    int Id,
    string ProductName,
    string? Category,
    string? GovernorateName,
    int HorizonMonths,
    string Trend,
    IReadOnlyList<string> MonthLabels,
    IReadOnlyList<double> MonthlyIndex,
    IReadOnlyList<int> PeakMonths,
    IReadOnlyList<string> Drivers,
    IReadOnlyList<string> Assumptions,
    string DataBasis,
    double Confidence,
    DateTime GeneratedAtUtc,
    bool FromCache);

public sealed record GenerateDemandForecastDto(
    string ProductName,
    string? Category = null,
    int? GovernorateId = null,
    int HorizonMonths = 12);