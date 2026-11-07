namespace Induva.Application.AI.Prompts;

public static class InvestmentPrompt
{
    public const string Version = "v1.0";

    public static string User(
        string productName,
        string? category,
        string governorateName,
        string? existingFactorySummary,
        decimal? desiredVolume,
        double? nearestMaterialSourceKm)
    {
        var data = new
        {
            product = PromptCommon.Clean(productName, 100),
            category = PromptCommon.Clean(category, 60),
            governorate = PromptCommon.Clean(governorateName, 60),
            existingFacility = PromptCommon.Clean(existingFactorySummary, 400),
            desiredMonthlyVolume = desiredVolume,
            nearestMaterialSourceKm = nearestMaterialSourceKm.HasValue ? Math.Round(nearestMaterialSourceKm.Value, 0) : (double?)null
        };

        return $$"""
            TASK: Propose realistic first-year industrial assumptions in EGP to manufacture {{productName}} in {{governorateName}}, Egypt.
            If an existing facility with machinery is described, capex should ONLY cover equipment that is missing or line modifications.
            Use nearestMaterialSourceKm to estimate realistic Egyptian inland transport costs per unit.
            Every unit cost must be for ONE unit of the product; state what that single unit is in rationale[0] (e.g. 'Per 1 standard 50kg bag').
            OUTPUT SCHEMA:
            {
              "assumptions": {
                "capexEgp": 0, "monthlyVolumeUnits": 0, "unitPriceEgp": 0, "unitMaterialCostEgp": 0,
                "unitLaborCostEgp": 0, "unitEnergyCostEgp": 0, "unitTransportCostEgp": 0,
                "monthlyFixedCostsEgp": 0, "rampUpMonths": 3
              },
              "capexBreakdown": [ { "item": "string", "egp": 0 } ],
              "rationale": [ "e.g. Unit is 1 box of 100pcs...", "rationale 2" ],
              "keyRisks": [ "string" ],
              "confidence": 0.0
            }
            {{PromptCommon.Wrap(data)}}
            """;
    }
}

