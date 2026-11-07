namespace Induva.Application.AI.Results;

public class DemandForecastResult
{
    public string Trend { get; set; } = "Stable";            // Rising | Stable | Falling
    public List<double> MonthlyIndex { get; set; } = [];     // N values, 0..100 (100 = peak demand in the window)
    public List<int> PeakMonths { get; set; } = [];          // 1..12
    public List<string> Drivers { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
    public string DataBasis { get; set; } = "GeneralKnowledge"; // PlatformData | GeneralKnowledge | Mixed
    public double Confidence { get; set; }
}

