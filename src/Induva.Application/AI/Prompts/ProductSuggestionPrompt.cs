using Induva.Domain.Constants;
using Induva.Domain.Entities;

namespace Induva.Application.AI.Prompts;

public static class ProductSuggestionPrompt
{
    public const string Version = "v1.0";

    public static string User(
        Factory f,
        IReadOnlyList<string> localRawMaterials,
        IReadOnlyList<DemandRequest> openDemand)
    {
        var data = new
        {
            factory = new
            {
                name = PromptCommon.Clean(f.Name, 100),
                sector = f.Sector.ToString(),
                governorate = f.Governorate?.NameEn ?? "Egypt",
                utilizationPercent = f.UtilizationPercent
            },
            capabilityTags = f.CapabilityProfile?.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            summary = PromptCommon.Clean(f.CapabilityProfile?.Summary, 600),
            machines = f.Machines.Select(m => new
            {
                name = PromptCommon.Clean(m.Name, 80),
                type = PromptCommon.Clean(m.MachineType, 60),
                quantity = m.Quantity,
                capacityPerHour = m.CapacityPerHour,
                capacityUnit = m.CapacityUnit
            }),
            idleWindows = f.IdleWindows.Select(w => new
            {
                startMonth = w.StartMonth,
                endMonth = w.EndMonth,
                availableCapacityPercent = w.AvailableCapacityPercent,
                note = PromptCommon.Clean(w.Note, 100)
            }),
            currentProducts = f.Products.Select(p => PromptCommon.Clean(p.Name, 100)),
            localRawMaterials = localRawMaterials.Take(15),
            openDemand = openDemand.Take(10).Select(d => new
            {
                title = PromptCommon.Clean(d.Title, 100),
                productName = PromptCommon.Clean(d.ProductName, 80),
                quantity = d.Quantity,
                unit = d.Unit
            })
        };

        var allowedTags = string.Join(", ", CapabilityTags.All);

        return $$"""
            TASK: Suggest up to 5 NEW products this factory could manufacture using ONLY its existing machines, preferably during its idle windows.
            Do not suggest products it already makes. Prefer products with local raw materials or visible demand in "openDemand".
            OUTPUT SCHEMA:
            { "suggestions": [ { "name":"", "category":"", "rationale":"max 40 words", "requiredMachines":["names from machines list"],
               "requiredRawMaterials":[""], "requiredCapabilityTags":["allowed-tag"], "estUnitCostEgp":0, "estUnitPriceEgp":0,
               "estMonthlyVolume":0, "demandLevel":"Low|Medium|High", "seasonalityNote":"", "confidence":0.0, "risks":["max 3"] } ],
              "assumptions": ["max 5"] }
            ALLOWED capability tags: {{allowedTags}}
            {{PromptCommon.Wrap(data)}}
            """;
    }
}
