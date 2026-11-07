namespace Induva.Application.AI.Prompts;

public static class DemandForecastPrompt
{
    public const string Version = "v1.0";

    public static string User(
        string productName,
        string? category,
        string? governorateName,
        int horizonMonths,
        string startingMonthName,
        IReadOnlyList<(int Month, int RequestCount, decimal TotalQuantity)> platformStats)
    {
        var data = new
        {
            productName = PromptCommon.Clean(productName, 100),
            category = PromptCommon.Clean(category, 60),
            governorate = PromptCommon.Clean(governorateName, 60) ?? "All Egypt",
            horizonMonths,
            startingMonth = startingMonthName,
            platformHistoricalStats = platformStats.Select(s => new
            {
                monthNumber = s.Month,
                openRequestsCount = s.RequestCount,
                totalQuantity = s.TotalQuantity
            })
        };

        var govText = string.IsNullOrWhiteSpace(governorateName) ? "" : $" for {governorateName} governorate";

        return $$"""
            TASK: Estimate relative monthly demand in Egypt{{govText}} for the product in <data>, for the next {{horizonMonths}} months starting {{startingMonthName}}.
            Consider seasonality (Ramadan, summer, school season, harvest cycles, export seasons) and the platform statistics provided.
            The platform statistics are SMALL and may not represent the market; if you rely mostly on general knowledge, set dataBasis accordingly and lower confidence.
            OUTPUT SCHEMA:
            { "trend":"Rising|Stable|Falling", "monthlyIndex":[ exactly {{horizonMonths}} numbers 0-100 ], "peakMonths":[1-12 month numbers],
              "drivers":["max 4"], "assumptions":["max 4"], "dataBasis":"PlatformData|GeneralKnowledge|Mixed", "confidence":0.0 }
            {{PromptCommon.Wrap(data)}}
            """;
    }
}
