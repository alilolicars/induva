namespace Induva.Application.AI.Prompts;

public static class MatchingPrompt
{
    public const string Version = "v1.0";

    public static string User(object sourceSummary, object candidatesList) => $$"""
        TASK: Below is a SOURCE and up to 8 pre-scored CANDIDATES (ruleScore 0-100). For each candidate return a small adjustment (-15..15) to the score based on fit details the rules cannot see (e.g., scale, seasonality, product compatibility, risks), up to 3 short reasons that cite ONLY the provided fields, and one concrete next step. Use ONLY candidateId values provided. Do not invent candidates.
        OUTPUT SCHEMA:
        { "matches": [ { "candidateId": 0, "adjustment": 0, "reasons": ["max 3 reasons"], "nextStep": "concrete action" } ] }
        {{PromptCommon.Wrap(new { source = sourceSummary, candidates = candidatesList })}}
        """;
}

