namespace Induva.Application.AI.Results;

public class MatchRerankResult
{
    public List<MatchRerankItem> Matches { get; set; } = [];
}

public class MatchRerankItem
{
    public int CandidateId { get; set; }
    public int Adjustment { get; set; } // -15..15
    public List<string> Reasons { get; set; } = []; // max 3
    public string? NextStep { get; set; }
}

