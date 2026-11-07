using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.AI.Prompts;

public static class FactoryAnalysisPrompt
{
    public const string Version = "factory-analysis.v1";

    public static object BuildInput(Factory f) => new
    {
        name = PromptCommon.Clean(f.Name, 120),
        sector = f.Sector.ToString(),
        governorate = f.Governorate?.NameEn ?? "",
        description = PromptCommon.Clean(f.Description, 1500),
        utilizationPercent = f.UtilizationPercent,
        machines = f.Machines.Take(30).Select(m => new
        {
            name = PromptCommon.Clean(m.Name, 100),
            type = PromptCommon.Clean(m.MachineType, 60),
            quantity = m.Quantity,
            capacityPerHour = m.CapacityPerHour,
            unit = m.CapacityUnit,
            notes = PromptCommon.Clean(m.Description, 200)
        }),
        idleWindows = f.IdleWindows.Select(w => new
        {
            fromMonth = w.StartMonth,
            toMonth = w.EndMonth,
            availableCapacityPercent = w.AvailableCapacityPercent
        }),
        currentProducts = f.Products.Take(20).Select(p => PromptCommon.Clean(p.Name, 100))
    };

    public static string User(Factory f) => $$"""
        TASK: Analyze the factory in <data> and describe what it can realistically manufacture with its existing machines.
        ALLOWED capability tags (use ONLY these, max 12): {{string.Join(", ", CapabilityTags.All)}}
        OUTPUT SCHEMA:
        {
          "summary": "string, max 60 words",
          "capabilityTags": ["allowed-tag"],
          "processes": [ { "name": "string", "machines": ["string"], "materialsProcessable": ["string"], "capacityNote": "string" } ],
          "strengths": ["string"],
          "constraints": ["string"],
          "underusedAssets": ["string"],
          "missingInformation": ["what the owner should add to improve this analysis"],
          "confidence": 0.0
        }
        Max 6 processes, max 4 items in each list. Base conclusions only on the data; do not invent machines.
        {{PromptCommon.Wrap(BuildInput(f))}}
        """;
}

