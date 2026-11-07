namespace Induva.Application.AI.Results;

public class FactoryAnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> CapabilityTags { get; set; } = new();
    public List<ProcessCapability> Processes { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
    public List<string> UnderusedAssets { get; set; } = new();
    public List<string> MissingInformation { get; set; } = new();
    public double Confidence { get; set; }
}

public class ProcessCapability
{
    public string Name { get; set; } = "";
    public List<string> Machines { get; set; } = new();
    public List<string> MaterialsProcessable { get; set; } = new();
    public string? CapacityNote { get; set; }
}

