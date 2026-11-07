namespace Induva.Application.AI.Results;

public class InvestmentAssumptionsResult
{
    public AssumptionValues Assumptions { get; set; } = new();
    public List<CapexItem> CapexBreakdown { get; set; } = [];
    public List<string> Rationale { get; set; } = [];
    public List<string> KeyRisks { get; set; } = [];
    public double Confidence { get; set; }
}

public class AssumptionValues
{
    public decimal CapexEgp { get; set; }
    public decimal MonthlyVolumeUnits { get; set; }
    public decimal UnitPriceEgp { get; set; }
    public decimal UnitMaterialCostEgp { get; set; }
    public decimal UnitLaborCostEgp { get; set; }
    public decimal UnitEnergyCostEgp { get; set; }
    public decimal UnitTransportCostEgp { get; set; }
    public decimal MonthlyFixedCostsEgp { get; set; }
    public int RampUpMonths { get; set; }
}

public class CapexItem
{
    public string Item { get; set; } = "";
    public decimal Egp { get; set; }
}

