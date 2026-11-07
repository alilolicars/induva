namespace Induva.Domain.Constants;

public static class CapabilityTags
{
    public static readonly string[] All =
    [
        "injection-molding",
        "extrusion",
        "blow-molding",
        "thermoforming",
        "cnc-machining",
        "metal-stamping",
        "welding",
        "casting",
        "sheet-metal-fabrication",
        "assembly",
        "textile-spinning",
        "weaving",
        "knitting",
        "dyeing-finishing",
        "garment-sewing",
        "leather-processing",
        "food-packaging",
        "fruit-vegetable-processing",
        "canning",
        "dairy-processing",
        "milling",
        "bakery-production",
        "beverage-bottling",
        "oil-pressing",
        "woodworking",
        "furniture-making",
        "stone-cutting",
        "ceramic-firing",
        "glass-forming",
        "paper-converting",
        "printing",
        "chemical-blending",
        "cosmetics-filling",
        "plastic-recycling",
        "3d-printing",
        "electronics-assembly"
    ];

    private static readonly HashSet<string> Set = new(All, StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string tag) => Set.Contains(tag.Trim());
}

